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
    public class UploadHomeworkModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public UploadHomeworkModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        public SelectList? GroupsSelectList { get; set; }
        public SelectList? LessonsSelectList { get; set; }

        [BindProperty]
        public int SelectedGroupId { get; set; }

        [BindProperty]
        public int SelectedLessonId { get; set; }

        [BindProperty]
        public string Description { get; set; } = string.Empty;

        [BindProperty]
        public string? Link { get; set; }

        [BindProperty]
        public DateTime? Deadline { get; set; }

        [BindProperty]
        public IFormFile? HomeworkFile { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);

            // Загружаем группы, в которых есть занятия этого учителя
            var teacherGroups = await _context.Lessons
                .Where(l => l.TeacherId == teacher.Id)
                .Select(l => l.Group)
                .Distinct()
                .ToListAsync();

            GroupsSelectList = new SelectList(teacherGroups, "Id", "Name");
        }

        public async Task<IActionResult> OnPostLoadLessonsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);

            var lessons = await _context.Lessons
                .Where(l => l.TeacherId == teacher.Id && l.GroupId == SelectedGroupId)
                .OrderByDescending(l => l.Date)
                .ToListAsync();

            LessonsSelectList = new SelectList(lessons, "Id", "Topic");

            // Перезагружаем группы
            var teacherGroups = await _context.Lessons
                .Where(l => l.TeacherId == teacher.Id)
                .Select(l => l.Group)
                .Distinct()
                .ToListAsync();

            GroupsSelectList = new SelectList(teacherGroups, "Id", "Name");

            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            if (SelectedGroupId == 0 || SelectedLessonId == 0 || string.IsNullOrEmpty(Description))
            {
                ModelState.AddModelError("", "Заполните все обязательные поля");
                return await OnPostLoadLessonsAsync();
            }

            var homework = new Homework
            {
                LessonId = SelectedLessonId,
                Description = Description,
                Link = Link,
                Deadline = Deadline,
                CreatedAt = DateTime.Now
            };

            if (HomeworkFile != null && HomeworkFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "homeworks");
                var yearFolder = Path.Combine(uploadsFolder, DateTime.Now.Year.ToString());
                var monthFolder = Path.Combine(yearFolder, DateTime.Now.Month.ToString("D2"));
                Directory.CreateDirectory(monthFolder);

                var fileName = $"{Guid.NewGuid()}_{HomeworkFile.FileName}";
                var filePath = Path.Combine(monthFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await HomeworkFile.CopyToAsync(stream);
                }

                homework.FilePath = $"/uploads/homeworks/{DateTime.Now.Year}/{DateTime.Now.Month:D2}/{fileName}";
            }

            _context.Homeworks.Add(homework);
            await _context.SaveChangesAsync();

            StatusMessage = "Домашнее задание успешно загружено";
            return RedirectToPage();
        }
    }
}