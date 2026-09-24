using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Data;
using ElectronicJournal.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ElectronicJournal.Pages.Student
{
    public class HomeworkModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public HomeworkModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        public List<Homework> Homeworks { get; set; } = new();
        public Dictionary<int, StudentHomework?> StudentSubmissions { get; set; } = new();
        public Models.Student? CurrentStudent { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchSubject { get; set; }

        // Для загрузки ДЗ
        [BindProperty]
        public SubmissionInputModel SubmissionInput { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class SubmissionInputModel
        {
            public int HomeworkId { get; set; }

            [Display(Name = "Комментарий к работе")]
            public string? StudentComment { get; set; }

            [Display(Name = "Файл с выполненным заданием")]
            public IFormFile? SubmissionFile { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            CurrentStudent = await _context.Students
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (CurrentStudent?.Group == null)
                return NotFound();

            // Загружаем домашние задания для группы
            var query = _context.Homeworks
                .Include(h => h.Lesson)
                    .ThenInclude(l => l.Teacher)
                        .ThenInclude(t => t.User)
                .Where(h => h.Lesson.GroupId == CurrentStudent.Group.Id)
                .AsQueryable();

            // Фильтр по дате
            if (!string.IsNullOrEmpty(SearchDate) && DateTime.TryParse(SearchDate, out var date))
            {
                query = query.Where(h => h.Lesson.Date.Date == date.Date);
            }

            // Фильтр по предмету/теме
            if (!string.IsNullOrEmpty(SearchSubject))
            {
                query = query.Where(h =>
                    h.Lesson.Topic.Contains(SearchSubject) ||
                    (h.Lesson.Teacher != null && h.Lesson.Teacher.Subject != null &&
                     h.Lesson.Teacher.Subject.Contains(SearchSubject)));
            }

            Homeworks = await query
                .OrderByDescending(h => h.Lesson.Date)
                .ToListAsync();

            // Загружаем сданные работы
            var homeworkIds = Homeworks.Select(h => h.Id).ToList();
            var submissions = await _context.StudentHomeworks
                .Where(sh => sh.StudentId == CurrentStudent.Id && homeworkIds.Contains(sh.HomeworkId))
                .ToListAsync();

            StudentSubmissions = submissions.ToDictionary(s => s.HomeworkId, s => s);

            return Page();
        }

        // ЗАГРУЗКА ВЫПОЛНЕННОГО ДЗ
        public async Task<IActionResult> OnPostSubmitHomeworkAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            CurrentStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (CurrentStudent == null)
                return NotFound("Студент не найден");

            // Проверяем, существует ли такое задание
            var homework = await _context.Homeworks
                .Include(h => h.Lesson)
                .FirstOrDefaultAsync(h => h.Id == SubmissionInput.HomeworkId);

            if (homework == null)
            {
                ErrorMessage = "Задание не найдено";
                return RedirectToPage();
            }

            // Проверяем, не сдавал ли уже студент эту работу
            var existingSubmission = await _context.StudentHomeworks
                .FirstOrDefaultAsync(sh =>
                    sh.StudentId == CurrentStudent.Id &&
                    sh.HomeworkId == SubmissionInput.HomeworkId);

            // Проверяем срок сдачи
            if (homework.Deadline.HasValue && homework.Deadline.Value < DateTime.Now && existingSubmission == null)
            {
                // Просрочено, но даем возможность сдать с пометкой
            }

            string? filePath = existingSubmission?.FilePath;

            // Если загружен новый файл
            if (SubmissionInput.SubmissionFile != null && SubmissionInput.SubmissionFile.Length > 0)
            {
                // Проверка размера файла (максимум 50 МБ)
                if (SubmissionInput.SubmissionFile.Length > 50 * 1024 * 1024)
                {
                    ErrorMessage = "Размер файла не должен превышать 50 МБ";
                    return RedirectToPage();
                }

                // Проверка расширения файла
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
                                                ".zip", ".rar", ".7z", ".txt", ".jpg", ".jpeg", ".png", ".bmp" };
                var fileExtension = Path.GetExtension(SubmissionInput.SubmissionFile.FileName).ToLower();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ErrorMessage = $"Недопустимый формат файла. Разрешены: {string.Join(", ", allowedExtensions)}";
                    return RedirectToPage();
                }

                // Создаем путь для сохранения
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "submissions");
                var yearFolder = Path.Combine(uploadsFolder, DateTime.Now.Year.ToString());
                var studentFolder = Path.Combine(yearFolder, $"Student_{CurrentStudent.Id}");
                Directory.CreateDirectory(studentFolder);

                // Удаляем старый файл, если есть
                if (!string.IsNullOrEmpty(existingSubmission?.FilePath))
                {
                    var oldFilePath = Path.Combine(_environment.WebRootPath, existingSubmission.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // Сохраняем новый файл
                var fileName = $"{Guid.NewGuid()}_{SubmissionInput.SubmissionFile.FileName}";
                var fullPath = Path.Combine(studentFolder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await SubmissionInput.SubmissionFile.CopyToAsync(stream);
                }

                filePath = $"/uploads/submissions/{DateTime.Now.Year}/Student_{CurrentStudent.Id}/{fileName}";
            }

            if (existingSubmission != null)
            {
                // Обновляем существующую запись
                existingSubmission.FilePath = filePath ?? existingSubmission.FilePath;
                existingSubmission.StudentComment = SubmissionInput.StudentComment;
                existingSubmission.SubmittedAt = DateTime.Now;
                existingSubmission.IsChecked = false; // Сбрасываем статус проверки
                existingSubmission.Grade = null; // Сбрасываем оценку
                existingSubmission.TeacherComment = null; // Сбрасываем комментарий учителя

                StatusMessage = "Работа обновлена и отправлена на повторную проверку";
            }
            else
            {
                // Создаем новую запись
                var submission = new StudentHomework
                {
                    StudentId = CurrentStudent.Id,
                    HomeworkId = SubmissionInput.HomeworkId,
                    FilePath = filePath,
                    StudentComment = SubmissionInput.StudentComment,
                    SubmittedAt = DateTime.Now,
                    IsChecked = false
                };

                _context.StudentHomeworks.Add(submission);
                StatusMessage = "Работа успешно отправлена на проверку!";
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        // УДАЛЕНИЕ СДАННОЙ РАБОТЫ
        public async Task<IActionResult> OnPostDeleteSubmissionAsync(int homeworkId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            CurrentStudent = await _context.Students
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (CurrentStudent == null)
                return NotFound();

            var submission = await _context.StudentHomeworks
                .FirstOrDefaultAsync(sh =>
                    sh.StudentId == CurrentStudent.Id &&
                    sh.HomeworkId == homeworkId);

            if (submission != null)
            {
                // Проверяем, не проверена ли уже работа
                if (submission.IsChecked)
                {
                    ErrorMessage = "Нельзя удалить уже проверенную работу";
                    return RedirectToPage();
                }

                // Удаляем файл
                if (!string.IsNullOrEmpty(submission.FilePath))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, submission.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.StudentHomeworks.Remove(submission);
                await _context.SaveChangesAsync();
                StatusMessage = "Отправленная работа удалена";
            }

            return RedirectToPage();
        }

        // Вспомогательные методы для отображения
        public string GetHomeworkStatus(int homeworkId, DateTime? deadline)
        {
            if (StudentSubmissions.TryGetValue(homeworkId, out var submission) && submission != null)
            {
                if (submission.Grade.HasValue && submission.IsChecked)
                    return $"Оценено: {submission.Grade}";
                else if (submission.IsChecked)
                    return "Проверено";
                else
                    return "Отправлено на проверку";
            }

            if (deadline.HasValue && deadline.Value < DateTime.Now)
                return "Просрочено!";

            return "Не сдано";
        }

        public string GetStatusClass(int homeworkId, DateTime? deadline)
        {
            if (StudentSubmissions.TryGetValue(homeworkId, out var submission) && submission != null)
            {
                if (submission.Grade.HasValue)
                {
                    return submission.Grade >= 4 ? "text-success" : submission.Grade == 3 ? "text-warning" : "text-danger";
                }
                return "text-primary";
            }

            if (deadline.HasValue && deadline.Value < DateTime.Now)
                return "text-danger fw-bold";

            return "text-muted";
        }
    }
}