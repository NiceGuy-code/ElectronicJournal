using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Data;
using ElectronicJournal.Models;
using Microsoft.AspNetCore.Identity;

namespace ElectronicJournal.Pages.Teacher
{
    [Authorize(Roles = "Teacher")]
    public class GroupJournalModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GroupJournalModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public SelectList? GroupsSelectList { get; set; }
        public List<Lesson> Lessons { get; set; } = new();
        public List<Models.Student> Students { get; set; } = new();
        public Dictionary<int, Dictionary<int, Grade>> Grades { get; set; } = new();
        public Dictionary<int, Dictionary<int, bool>> Attendance { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int SelectedGroupId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Month { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Year { get; set; }

        public DateTime CurrentMonth { get; set; } = DateTime.Today;

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);

            // Загружаем группы учителя
            var teacherGroups = await _context.Lessons
                .Where(l => l.TeacherId == teacher.Id)
                .Select(l => l.Group)
                .Distinct()
                .ToListAsync();

            GroupsSelectList = new SelectList(teacherGroups, "Id", "Name");

            if (SelectedGroupId == 0)
                return Page();

            if (Month.HasValue && Year.HasValue)
            {
                CurrentMonth = new DateTime(Year.Value, Month.Value, 1);
            }

            var startDate = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Загружаем занятия
            Lessons = await _context.Lessons
                .Where(l => l.GroupId == SelectedGroupId
                    && l.TeacherId == teacher.Id
                    && l.Date >= startDate
                    && l.Date <= endDate)
                .OrderBy(l => l.Date)
                .ToListAsync();

            // Загружаем студентов группы
            Students = await _context.Students
                .Include(s => s.User)
                .Where(s => s.GroupId == SelectedGroupId)
                .OrderBy(s => s.User.LastName)
                .ThenBy(s => s.User.FirstName)
                .ToListAsync();

            // Загружаем все оценки для этих занятий
            var lessonIds = Lessons.Select(l => l.Id).ToList();
            var allGrades = await _context.Grades
                .Where(g => lessonIds.Contains(g.LessonId) &&
                    Students.Select(s => s.Id).Contains(g.StudentId))
                .ToListAsync();

            foreach (var student in Students)
            {
                Grades[student.Id] = new Dictionary<int, Grade>();
                foreach (var lessonId in lessonIds)
                {
                    var grade = allGrades.FirstOrDefault(g =>
                        g.StudentId == student.Id && g.LessonId == lessonId);
                    Grades[student.Id][lessonId] = grade;
                }
            }

            // Загружаем посещаемость
            var allAttendance = await _context.LessonAttendances
                .Where(a => lessonIds.Contains(a.LessonId) &&
                    Students.Select(s => s.Id).Contains(a.StudentId))
                .ToListAsync();

            foreach (var student in Students)
            {
                Attendance[student.Id] = new Dictionary<int, bool>();
                foreach (var lessonId in lessonIds)
                {
                    var att = allAttendance.FirstOrDefault(a =>
                        a.StudentId == student.Id && a.LessonId == lessonId);
                    Attendance[student.Id][lessonId] = att?.IsPresent ?? false;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateCellAsync(
            int studentId, int lessonId, string field, string value)
        {
            var existingGrade = await _context.Grades
                .FirstOrDefaultAsync(g => g.StudentId == studentId && g.LessonId == lessonId);

            if (existingGrade == null)
            {
                existingGrade = new Grade
                {
                    StudentId = studentId,
                    LessonId = lessonId,
                    CreatedAt = DateTime.Now
                };
                _context.Grades.Add(existingGrade);
            }

            switch (field)
            {
                case "lessonGrade":
                    existingGrade.LessonGrade = int.TryParse(value, out var lg) ? lg : null;
                    break;
                case "homeworkGrade":
                    existingGrade.HomeworkGrade = int.TryParse(value, out var hg) ? hg : null;
                    break;
                case "isNotCertified":
                    existingGrade.IsNotCertified = value == "true";
                    break;
            }

            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }
    }
}