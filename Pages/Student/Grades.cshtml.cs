using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Data;
using ElectronicJournal.Models;
using Microsoft.AspNetCore.Identity;

namespace ElectronicJournal.Pages.Student
{
    public class GradesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GradesModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Lesson> Lessons { get; set; } = new();
        public Dictionary<int, Grade?> Grades { get; set; } = new();
        public Dictionary<int, bool> Attendance { get; set; } = new();
        public Models.Student? CurrentStudent { get; set; }
        public DateTime CurrentMonth { get; set; } = DateTime.Today;
        public DateTime PreviousMonth { get; set; }
        public DateTime NextMonth { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Month { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Year { get; set; }

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

            if (Month.HasValue && Year.HasValue)
            {
                CurrentMonth = new DateTime(Year.Value, Month.Value, 1);
            }

            PreviousMonth = CurrentMonth.AddMonths(-1);
            NextMonth = CurrentMonth.AddMonths(1);

            var startDate = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Получаем занятия
            Lessons = await _context.Lessons
                .Include(l => l.Teacher)
                    .ThenInclude(t => t.User)
                .Where(l => l.GroupId == CurrentStudent.Group.Id
                    && l.Date >= startDate
                    && l.Date <= endDate)
                .OrderBy(l => l.Date)
                .ToListAsync();

            // Получаем оценки для каждого занятия
            foreach (var lesson in Lessons)
            {
                var grade = await _context.Grades
                    .FirstOrDefaultAsync(g => g.StudentId == CurrentStudent.Id && g.LessonId == lesson.Id);

                Grades[lesson.Id] = grade;
            }

            // Получаем посещаемость
            var attendances = await _context.LessonAttendances
                .Where(a => a.StudentId == CurrentStudent.Id
                    && Lessons.Select(l => l.Id).Contains(a.LessonId))
                .ToListAsync();

            foreach (var attendance in attendances)
            {
                Attendance[attendance.LessonId] = attendance.IsPresent;
            }

            return Page();
        }

        // Методы для отображения оценок
        public string GetLessonGradeDisplay(int lessonId)
        {
            if (Grades.TryGetValue(lessonId, out var grade) && grade != null)
            {
                if (grade.IsNotCertified)
                    return "н/а";

                if (grade.LessonGrade.HasValue)
                    return grade.LessonGrade.Value.ToString();
            }

            // Проверяем посещаемость
            if (Attendance.TryGetValue(lessonId, out var isPresent) && !isPresent)
                return "н/б";

            return "-";
        }

        public string GetHomeworkGradeDisplay(int lessonId)
        {
            if (Grades.TryGetValue(lessonId, out var grade) && grade != null)
            {
                if (grade.IsNotCertified)
                    return "н/а";

                if (grade.HomeworkGrade.HasValue)
                    return grade.HomeworkGrade.Value.ToString();

                return "нет";
            }

            return "нет";
        }

        public string GetGradeClass(string gradeDisplay)
        {
            return gradeDisplay switch
            {
                "5" => "grade-5",
                "4" => "grade-4",
                "3" => "grade-3",
                "2" => "grade-2",
                "1" => "grade-1",
                "н/б" => "grade-nb",
                "н/а" => "grade-nk",
                _ => ""
            };
        }
    }
}