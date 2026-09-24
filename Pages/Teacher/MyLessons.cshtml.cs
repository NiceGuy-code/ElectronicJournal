using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Data;
using ElectronicJournal.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ElectronicJournal.Pages.Teacher
{
    [Authorize(Roles = "Teacher")]
    public class MyLessonsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public MyLessonsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        public List<Lesson> Lessons { get; set; } = new();
        public Models.Teacher? CurrentTeacher { get; set; }
        public DateTime CurrentMonth { get; set; } = DateTime.Today;
        public DateTime PreviousMonth { get; set; }
        public DateTime NextMonth { get; set; }

        public Lesson? SelectedLesson { get; set; }
        public List<StudentViewModel> Students { get; set; } = new();
        public List<Homework> LessonHomeworks { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? Month { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Year { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedLessonId { get; set; }

        [BindProperty]
        public HomeworkInputModel HomeworkInput { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class StudentViewModel
        {
            public int StudentId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public bool IsPresent { get; set; }
            public int? LessonGrade { get; set; }
            public int? HomeworkGrade { get; set; }
            public bool IsNotCertified { get; set; }
            public List<StudentHomeworkViewModel> Submissions { get; set; } = new();
        }

        public class StudentHomeworkViewModel
        {
            public int Id { get; set; }
            public string? Description { get; set; }
            public string? FilePath { get; set; }
            public string? StudentComment { get; set; }
            public string? TeacherComment { get; set; }
            public DateTime SubmittedAt { get; set; }
            public int? Grade { get; set; }
            public bool IsChecked { get; set; }
        }

        public class HomeworkInputModel
        {
            [Required(ErrorMessage = "Описание задания обязательно")]
            public string Description { get; set; } = string.Empty;
            public string? Link { get; set; }
            public DateTime? Deadline { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login");

            CurrentTeacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (CurrentTeacher == null) return NotFound("Учитель не найден");

            if (Month.HasValue && Year.HasValue)
                CurrentMonth = new DateTime(Year.Value, Month.Value, 1);
            else { Month = CurrentMonth.Month; Year = CurrentMonth.Year; }

            PreviousMonth = CurrentMonth.AddMonths(-1);
            NextMonth = CurrentMonth.AddMonths(1);

            var startDate = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            Lessons = await _context.Lessons
                .Include(l => l.Group)
                .Where(l => l.TeacherId == CurrentTeacher.Id && l.Date >= startDate && l.Date <= endDate)
                .OrderBy(l => l.Date).ThenBy(l => l.StartTime)
                .ToListAsync();

            if (SelectedLessonId.HasValue)
                await LoadLessonDetailsAsync(SelectedLessonId.Value);

            return Page();
        }

        private async Task LoadLessonDetailsAsync(int lessonId)
        {
            SelectedLesson = await _context.Lessons.Include(l => l.Group).FirstOrDefaultAsync(l => l.Id == lessonId);
            if (SelectedLesson?.Group == null) return;

            var groupStudents = await _context.Students.Include(s => s.User)
                .Where(s => s.GroupId == SelectedLesson.GroupId)
                .OrderBy(s => s.User.LastName).ThenBy(s => s.User.FirstName).ToListAsync();

            var existingGrades = await _context.Grades.Where(g => g.LessonId == lessonId).ToListAsync();
            var existingAttendance = await _context.LessonAttendances.Where(a => a.LessonId == lessonId).ToListAsync();
            LessonHomeworks = await _context.Homeworks.Where(h => h.LessonId == lessonId).ToListAsync();

            var homeworkIds = LessonHomeworks.Select(h => h.Id).ToList();
            var submissions = await _context.StudentHomeworks
                .Where(sh => homeworkIds.Contains(sh.HomeworkId)).Include(sh => sh.Homework).ToListAsync();

            Students = groupStudents.Select(s =>
            {
                var grade = existingGrades.FirstOrDefault(g => g.StudentId == s.Id);
                var attendance = existingAttendance.FirstOrDefault(a => a.StudentId == s.Id);
                return new StudentViewModel
                {
                    StudentId = s.Id,
                    FullName = s.User?.FullName ?? "Неизвестный",
                    IsPresent = attendance?.IsPresent ?? false,
                    LessonGrade = grade?.LessonGrade,
                    HomeworkGrade = grade?.HomeworkGrade,
                    IsNotCertified = grade?.IsNotCertified ?? false,
                    Submissions = submissions.Where(sub => sub.StudentId == s.Id).Select(sub => new StudentHomeworkViewModel
                    {
                        Id = sub.Id,
                        Description = sub.Homework?.Description ?? "",
                        FilePath = sub.FilePath,
                        StudentComment = sub.StudentComment,
                        TeacherComment = sub.TeacherComment,
                        SubmittedAt = sub.SubmittedAt,
                        Grade = sub.Grade,
                        IsChecked = sub.IsChecked
                    }).ToList()
                };
            }).ToList();
        }

        // ==================== ОБЩИЙ ОБРАБОТЧИК POST ====================
        public async Task<IActionResult> OnPostAsync()
        {
            var handler = Request.Form["handler"].FirstOrDefault();

            if (handler == "SaveAttendance")
                return await HandleSaveAttendance();
            else if (handler == "SaveGrades")
                return await HandleSaveGrades();
            else if (handler == "AddHomework")
                return await HandleAddHomework();
            else if (handler == "DeleteHomework")
                return await HandleDeleteHomework();

            return Page();
        }

        // ==================== СОХРАНЕНИЕ ПОСЕЩАЕМОСТИ ====================
        private async Task<IActionResult> HandleSaveAttendance()
        {
            var lessonIdStr = Request.Form["lessonId"].FirstOrDefault();
            if (!int.TryParse(lessonIdStr, out int lessonId))
            {
                ErrorMessage = "Неверный ID занятия";
                return RedirectToPage(new { month = Month, year = Year });
            }

            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("attendance["))
                {
                    var studentIdStr = key.Replace("attendance[", "").Replace("]", "");
                    if (int.TryParse(studentIdStr, out int studentId))
                    {
                        var value = Request.Form[key].FirstOrDefault();
                        bool isPresent = value == "true";

                        var existing = await _context.LessonAttendances
                            .FirstOrDefaultAsync(a => a.StudentId == studentId && a.LessonId == lessonId);

                        if (existing != null)
                        {
                            existing.IsPresent = isPresent;
                            existing.MarkedAt = DateTime.Now;
                        }
                        else
                        {
                            _context.LessonAttendances.Add(new LessonAttendance
                            {
                                StudentId = studentId,
                                LessonId = lessonId,
                                IsPresent = isPresent,
                                MarkedAt = DateTime.Now
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            StatusMessage = "Посещаемость сохранена";
            return RedirectToPage(new { month = Month, year = Year, selectedLessonId = lessonId });
        }

        // ==================== СОХРАНЕНИЕ ОЦЕНОК ====================
        private async Task<IActionResult> HandleSaveGrades()
        {
            var lessonIdStr = Request.Form["lessonId"].FirstOrDefault();
            if (!int.TryParse(lessonIdStr, out int lessonId))
            {
                ErrorMessage = "Неверный ID занятия";
                return RedirectToPage(new { month = Month, year = Year });
            }

            // Собираем все studentId из формы
            var studentIds = new HashSet<int>();
            foreach (var key in Request.Form.Keys)
            {
                if (key.StartsWith("lessonGrades["))
                {
                    var idStr = key.Replace("lessonGrades[", "").Replace("]", "");
                    if (int.TryParse(idStr, out int sid)) studentIds.Add(sid);
                }
                else if (key.StartsWith("homeworkGrades["))
                {
                    var idStr = key.Replace("homeworkGrades[", "").Replace("]", "");
                    if (int.TryParse(idStr, out int sid)) studentIds.Add(sid);
                }
                else if (key.StartsWith("notCertified["))
                {
                    var idStr = key.Replace("notCertified[", "").Replace("]", "");
                    if (int.TryParse(idStr, out int sid)) studentIds.Add(sid);
                }
            }

            foreach (var studentId in studentIds)
            {
                int? lessonGrade = null;
                int? homeworkGrade = null;
                bool notCertified = false;

                var lgStr = Request.Form[$"lessonGrades[{studentId}]"].FirstOrDefault();
                if (int.TryParse(lgStr, out int lg)) lessonGrade = lg;

                var hgStr = Request.Form[$"homeworkGrades[{studentId}]"].FirstOrDefault();
                if (int.TryParse(hgStr, out int hg)) homeworkGrade = hg;

                var ncStr = Request.Form[$"notCertified[{studentId}]"].FirstOrDefault();
                notCertified = ncStr == "true";

                var existing = await _context.Grades
                    .FirstOrDefaultAsync(g => g.StudentId == studentId && g.LessonId == lessonId);

                if (existing != null)
                {
                    existing.LessonGrade = lessonGrade;
                    existing.HomeworkGrade = homeworkGrade;
                    existing.IsNotCertified = notCertified;
                    existing.CreatedAt = DateTime.Now;
                }
                else if (lessonGrade.HasValue || homeworkGrade.HasValue || notCertified)
                {
                    _context.Grades.Add(new Grade
                    {
                        StudentId = studentId,
                        LessonId = lessonId,
                        LessonGrade = lessonGrade,
                        HomeworkGrade = homeworkGrade,
                        IsNotCertified = notCertified,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            StatusMessage = "Оценки сохранены";
            return RedirectToPage(new { month = Month, year = Year, selectedLessonId = lessonId });
        }

        // ==================== ДОБАВЛЕНИЕ ДЗ ====================
        private async Task<IActionResult> HandleAddHomework()
        {
            var lessonIdStr = Request.Form["lessonId"].FirstOrDefault();
            if (!int.TryParse(lessonIdStr, out int lessonId))
            {
                ErrorMessage = "Неверный ID занятия";
                return RedirectToPage(new { month = Month, year = Year });
            }

            var description = Request.Form["HomeworkInput.Description"].FirstOrDefault();
            if (string.IsNullOrEmpty(description))
            {
                ErrorMessage = "Заполните описание задания";
                return RedirectToPage(new { month = Month, year = Year, selectedLessonId = lessonId });
            }

            var homework = new Homework
            {
                LessonId = lessonId,
                Description = description,
                Link = Request.Form["HomeworkInput.Link"].FirstOrDefault(),
                CreatedAt = DateTime.Now
            };

            var deadlineStr = Request.Form["HomeworkInput.Deadline"].FirstOrDefault();
            if (DateTime.TryParse(deadlineStr, out DateTime deadline))
                homework.Deadline = deadline;

            // Файл
            var file = Request.Form.Files.GetFile("homeworkFile");
            if (file != null && file.Length > 0)
            {
                try
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "homeworks");
                    var yearFolder = Path.Combine(uploadsFolder, DateTime.Now.Year.ToString());
                    var monthFolder = Path.Combine(yearFolder, DateTime.Now.Month.ToString("D2"));
                    if (!Directory.Exists(monthFolder)) Directory.CreateDirectory(monthFolder);

                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(monthFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    homework.FilePath = $"/uploads/homeworks/{DateTime.Now.Year}/{DateTime.Now.Month:D2}/{fileName}";
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Ошибка сохранения файла: {ex.Message}";
                    return RedirectToPage(new { month = Month, year = Year, selectedLessonId = lessonId });
                }
            }

            _context.Homeworks.Add(homework);
            await _context.SaveChangesAsync();
            StatusMessage = "Домашнее задание добавлено";
            return RedirectToPage(new { month = Month, year = Year, selectedLessonId = lessonId });
        }

        // ==================== УДАЛЕНИЕ ДЗ ====================
        private async Task<IActionResult> HandleDeleteHomework()
        {
            var homeworkIdStr = Request.Form["homeworkId"].FirstOrDefault();
            var lessonIdStr = Request.Form["lessonId"].FirstOrDefault();

            if (int.TryParse(homeworkIdStr, out int homeworkId))
            {
                var homework = await _context.Homeworks.FindAsync(homeworkId);
                if (homework != null)
                {
                    if (!string.IsNullOrEmpty(homework.FilePath))
                    {
                        var filePath = Path.Combine(_environment.WebRootPath, homework.FilePath.TrimStart('/'));
                        if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                    }
                    _context.Homeworks.Remove(homework);
                    await _context.SaveChangesAsync();
                    StatusMessage = "Домашнее задание удалено";
                }
            }

            int.TryParse(lessonIdStr, out int lessonId);
            return RedirectToPage(new { month = Month, year = Year, selectedLessonId = lessonId });
        }
    }
}