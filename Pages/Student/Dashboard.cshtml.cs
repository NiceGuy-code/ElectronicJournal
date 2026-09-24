using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Data;
using ElectronicJournal.Models;
using Microsoft.AspNetCore.Identity;

namespace ElectronicJournal.Pages.Student
{
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Свойства для отображения статистики
        public double AverageGrade { get; set; }
        public double AttendancePercentage { get; set; }
        public int TotalLessons { get; set; }
        public int AttendedLessons { get; set; }
        public int TotalHomeworks { get; set; }
        public int CompletedHomeworks { get; set; }
        public Models.Student? CurrentStudent { get; set; }
        public Models.Group? StudentGroup { get; set; }
        public List<Announcement> RecentAnnouncements { get; set; } = new();
        public List<Lesson> UpcomingLessons { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Получаем текущего пользователя
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            // Получаем студента с группой
            CurrentStudent = await _context.Students
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (CurrentStudent == null)
                return RedirectToPage("/Account/Login");

            StudentGroup = CurrentStudent.Group;

            if (StudentGroup != null)
            {
                // Все занятия группы
                var allLessons = await _context.Lessons
                    .Where(l => l.GroupId == StudentGroup.Id)
                    .ToListAsync();

                TotalLessons = allLessons.Count;

                // Получаем все оценки студента
                var grades = await _context.Grades
                    .Where(g => g.StudentId == CurrentStudent.Id)
                    .ToListAsync();

                // Средний балл за занятия (только для занятий, где нет "н/а")
                var validLessonGrades = grades
                    .Where(g => g.LessonGrade.HasValue && !g.IsNotCertified)
                    .Select(g => g.LessonGrade.Value)
                    .ToList();

                // Средний балл за ДЗ (только выполненные и с оценкой)
                var validHomeworkGrades = grades
                    .Where(g => g.HomeworkGrade.HasValue && !g.IsNotCertified)
                    .Select(g => g.HomeworkGrade.Value)
                    .ToList();

                // Общий средний балл
                var allGrades = validLessonGrades.Concat(validHomeworkGrades).ToList();
                AverageGrade = allGrades.Any() ? Math.Round(allGrades.Average(), 2) : 0;

                // Посещаемость
                var attendances = await _context.LessonAttendances
                    .Where(a => a.StudentId == CurrentStudent.Id)
                    .ToListAsync();

                AttendedLessons = attendances.Count(a => a.IsPresent);

                if (TotalLessons > 0)
                {
                    // Процент посещаемости от всех занятий
                    AttendancePercentage = Math.Round((double)AttendedLessons / TotalLessons * 100, 1);
                }

                // Статистика по домашним заданиям
                var allHomeworks = await _context.Homeworks
                    .Where(h => h.Lesson.GroupId == StudentGroup.Id)
                    .ToListAsync();

                TotalHomeworks = allHomeworks.Count;

                CompletedHomeworks = await _context.StudentHomeworks
                    .Where(sh => sh.StudentId == CurrentStudent.Id)
                    .CountAsync();

                // Последние объявления
                RecentAnnouncements = await _context.Announcements
                    .Include(a => a.Author)
                    .Where(a => a.IsActive && (a.IsForAll || a.GroupId == StudentGroup.Id))
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                // Ближайшие занятия
                var today = DateTime.Today;
                UpcomingLessons = await _context.Lessons
                    .Include(l => l.Teacher)
                        .ThenInclude(t => t.User)
                    .Where(l => l.GroupId == StudentGroup.Id && l.Date >= today)
                    .OrderBy(l => l.Date)
                    .ThenBy(l => l.StartTime)
                    .Take(5)
                    .ToListAsync();
            }

            return Page();
        }
    }
}