using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Data;
using ElectronicJournal.Models;
using Microsoft.AspNetCore.Identity;

namespace ElectronicJournal.Pages.Student
{
    public class ScheduleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ScheduleModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Lesson> Lessons { get; set; } = new();
        public Models.Student? CurrentStudent { get; set; }
        public DateTime CurrentMonth { get; set; } = DateTime.Today;
        public DateTime PreviousMonth { get; set; }
        public DateTime NextMonth { get; set; }
        public string CurrentMonthName { get; set; } = string.Empty;

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
                return NotFound("Студент не привязан к группе");

            // Определяем текущий месяц
            if (Month.HasValue && Year.HasValue)
            {
                CurrentMonth = new DateTime(Year.Value, Month.Value, 1);
            }

            PreviousMonth = CurrentMonth.AddMonths(-1);
            NextMonth = CurrentMonth.AddMonths(1);
            CurrentMonthName = CurrentMonth.ToString("MMMM yyyy");

            // Получаем занятия на текущий месяц
            var startDate = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            Lessons = await _context.Lessons
                .Include(l => l.Teacher)
                    .ThenInclude(t => t.User)
                .Where(l => l.GroupId == CurrentStudent.Group.Id
                    && l.Date >= startDate
                    && l.Date <= endDate)
                .OrderBy(l => l.Date)
                .ThenBy(l => l.StartTime)
                .ToListAsync();

            return Page();
        }

        // Метод для построения календаря
        public List<List<DateTime>> GetCalendarWeeks()
        {
            var weeks = new List<List<DateTime>>();
            var firstDay = new DateTime(CurrentMonth.Year, CurrentMonth.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            // Начинаем с понедельника
            var startDate = firstDay.AddDays(-(int)firstDay.DayOfWeek + 1);
            if (firstDay.DayOfWeek == DayOfWeek.Sunday)
                startDate = firstDay.AddDays(-6);

            var currentDate = startDate;

            while (currentDate <= lastDay || currentDate.DayOfWeek != DayOfWeek.Monday)
            {
                var week = new List<DateTime>();
                for (int i = 0; i < 7; i++)
                {
                    week.Add(currentDate);
                    currentDate = currentDate.AddDays(1);
                }
                weeks.Add(week);

                if (currentDate > lastDay && currentDate.DayOfWeek == DayOfWeek.Monday)
                    break;
            }

            return weeks;
        }

        public List<Lesson> GetLessonsForDay(DateTime date)
        {
            return Lessons.Where(l => l.Date.Date == date.Date).ToList();
        }
    }
}