using ElectronicJournal.Data;
using ElectronicJournal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ElectronicJournal.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ScheduleModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ScheduleModel> _logger;

        public ScheduleModel(ApplicationDbContext context, ILogger<ScheduleModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public SelectList? GroupsSelectList { get; set; }
        public SelectList? TeachersSelectList { get; set; }
        public List<Lesson> Lessons { get; set; } = new();

        [BindProperty]
        public LessonInputModel Input { get; set; } = new();

        [BindProperty]
        public BulkLessonInputModel BulkInput { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedGroupId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class LessonInputModel
        {
            public int? Id { get; set; }

            [Required(ErrorMessage = "Дата обязательна")]
            public DateTime Date { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "Время начала обязательно")]
            public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

            [Required(ErrorMessage = "Время окончания обязательно")]
            public TimeSpan EndTime { get; set; } = new TimeSpan(10, 30, 0);

            //[Required(ErrorMessage = "Тема обязательна")]
            [StringLength(200)]
            public string Topic { get; set; } = string.Empty;

            public string? Description { get; set; }

            [Required(ErrorMessage = "Группа обязательна")]
            public int GroupId { get; set; }

            [Required(ErrorMessage = "Преподаватель обязателен")]
            public int TeacherId { get; set; }
        }

        public class BulkLessonInputModel
        {
            [Required(ErrorMessage = "Выберите группу")]
            public int GroupId { get; set; }

            [Required(ErrorMessage = "Выберите преподавателя")]
            public int TeacherId { get; set; }

            //[Required(ErrorMessage = "Укажите тему")]
            public string Topic { get; set; } = string.Empty;

            public string? Description { get; set; }

            [Required(ErrorMessage = "Укажите дату начала")]
            public DateTime StartDate { get; set; } = DateTime.Today;

            [Required(ErrorMessage = "Укажите дату окончания")]
            public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);

            [Required(ErrorMessage = "Укажите время начала")]
            public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

            [Required(ErrorMessage = "Укажите время окончания")]
            public TimeSpan EndTime { get; set; } = new TimeSpan(10, 30, 0);

            public bool Monday { get; set; }
            public bool Tuesday { get; set; }
            public bool Wednesday { get; set; }
            public bool Thursday { get; set; }
            public bool Friday { get; set; }
            public bool Saturday { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadSelectLists();

            if (SelectedGroupId.HasValue && SelectedGroupId.Value > 0)
            {
                await LoadLessons();
            }
        }

        private async Task LoadSelectLists()
        {
            var groups = await _context.Groups.Where(g => g.IsActive).ToListAsync();
            GroupsSelectList = new SelectList(groups, "Id", "Name");

            var teachers = await _context.Teachers
                .Include(t => t.User)
                .ToListAsync();
            TeachersSelectList = new SelectList(teachers, "Id", "User.FullName");
        }

        private async Task LoadLessons()
        {
            var query = _context.Lessons
                .Include(l => l.Teacher)
                    .ThenInclude(t => t.User)
                .Include(l => l.Group)
                .AsQueryable();

            if (SelectedGroupId.HasValue && SelectedGroupId.Value > 0)
            {
                query = query.Where(l => l.GroupId == SelectedGroupId.Value);
            }

            if (SelectedDate.HasValue)
            {
                query = query.Where(l => l.Date.Date == SelectedDate.Value.Date);
            }
            else
            {
                // Если дата не выбрана, показываем текущую неделю
                var today = DateTime.Today;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1);
                var endOfWeek = startOfWeek.AddDays(7);
                query = query.Where(l => l.Date >= startOfWeek && l.Date <= endOfWeek);
            }

            Lessons = await query
                .OrderBy(l => l.Date)
                .ThenBy(l => l.StartTime)
                .ToListAsync();
        }

        // СОЗДАНИЕ ОДНОГО ЗАНЯТИЯ
        public async Task<IActionResult> OnPostCreateAsync()
        {
            _logger.LogInformation("OnPostCreateAsync вызван");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState невалиден");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning($"Ошибка валидации: {error.ErrorMessage}");
                }
                await LoadSelectLists();
                await LoadLessons();
                return Page();
            }

            try
            {
                var lesson = new Lesson
                {
                    Date = Input.Date,
                    StartTime = Input.StartTime,
                    EndTime = Input.EndTime,
                    Topic = Input.Topic,
                    Description = Input.Description,
                    GroupId = Input.GroupId,
                    TeacherId = Input.TeacherId
                };

                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Занятие создано: ID={lesson.Id}, Topic={lesson.Topic}");

                StatusMessage = $"Занятие '{lesson.Topic}' успешно добавлено в расписание";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании занятия");
                ErrorMessage = $"Ошибка при создании занятия: {ex.Message}";
            }

            return RedirectToPage(new { selectedGroupId = Input.GroupId });
        }

        // МАССОВОЕ СОЗДАНИЕ ЗАНЯТИЙ
        public async Task<IActionResult> OnPostCreateBulkAsync()
        {
            _logger.LogInformation("OnPostCreateBulkAsync вызван");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState невалиден для массового создания");
                await LoadSelectLists();
                return Page();
            }

            try
            {
                int createdCount = 0;
                var currentDate = BulkInput.StartDate;

                while (currentDate <= BulkInput.EndDate)
                {
                    var dayOfWeek = (int)currentDate.DayOfWeek;
                    if (dayOfWeek == 0) dayOfWeek = 7; // Воскресенье = 7

                    bool shouldCreate = (dayOfWeek == 1 && BulkInput.Monday) ||
                                       (dayOfWeek == 2 && BulkInput.Tuesday) ||
                                       (dayOfWeek == 3 && BulkInput.Wednesday) ||
                                       (dayOfWeek == 4 && BulkInput.Thursday) ||
                                       (dayOfWeek == 5 && BulkInput.Friday) ||
                                       (dayOfWeek == 6 && BulkInput.Saturday);

                    if (shouldCreate)
                    {
                        var lesson = new Lesson
                        {
                            Date = currentDate,
                            StartTime = BulkInput.StartTime,
                            EndTime = BulkInput.EndTime,
                            Topic = BulkInput.Topic,
                            Description = BulkInput.Description,
                            GroupId = BulkInput.GroupId,
                            TeacherId = BulkInput.TeacherId
                        };

                        _context.Lessons.Add(lesson);
                        createdCount++;
                    }

                    currentDate = currentDate.AddDays(1);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Массово создано занятий: {createdCount}");

                StatusMessage = $"Успешно создано занятий: {createdCount}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при массовом создании занятий");
                ErrorMessage = $"Ошибка: {ex.Message}";
            }

            return RedirectToPage(new { selectedGroupId = BulkInput.GroupId });
        }

        // РЕДАКТИРОВАНИЕ ЗАНЯТИЯ
        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!Input.Id.HasValue)
            {
                ErrorMessage = "ID занятия не указан";
                return RedirectToPage();
            }

            var lesson = await _context.Lessons.FindAsync(Input.Id.Value);
            if (lesson == null)
            {
                ErrorMessage = "Занятие не найдено";
                return RedirectToPage();
            }

            lesson.Date = Input.Date;
            lesson.StartTime = Input.StartTime;
            lesson.EndTime = Input.EndTime;
            lesson.Topic = Input.Topic;
            lesson.Description = Input.Description;
            lesson.GroupId = Input.GroupId;
            lesson.TeacherId = Input.TeacherId;

            await _context.SaveChangesAsync();
            StatusMessage = $"Занятие '{lesson.Topic}' обновлено";

            return RedirectToPage(new { selectedGroupId = Input.GroupId });
        }

        // УДАЛЕНИЕ ЗАНЯТИЯ
        public async Task<IActionResult> OnPostDeleteAsync(int lessonId)
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson != null)
            {
                var groupId = lesson.GroupId;
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();
                StatusMessage = "Занятие удалено из расписания";
                return RedirectToPage(new { selectedGroupId = groupId });
            }

            ErrorMessage = "Занятие не найдено";
            return RedirectToPage();
        }

        // AJAX: Получение данных занятия для редактирования
        public async Task<IActionResult> OnGetGetLessonAsync(int lessonId)
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
                return NotFound();

            return new JsonResult(new
            {
                id = lesson.Id,
                date = lesson.Date.ToString("yyyy-MM-dd"),
                startTime = lesson.StartTime.ToString(@"hh\:mm"),
                endTime = lesson.EndTime.ToString(@"hh\:mm"),
                topic = lesson.Topic,
                description = lesson.Description ?? "",
                groupId = lesson.GroupId,
                teacherId = lesson.TeacherId
            });
        }
    }
}