using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Data;
using ElectronicJournal.Models;
using System.ComponentModel.DataAnnotations;

namespace ElectronicJournal.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class GroupsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GroupsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Group> Groups { get; set; } = new();
        public SelectList? TeachersSelectList { get; set; }
        public Dictionary<int, int> StudentCounts { get; set; } = new();

        [BindProperty]
        public GroupInputModel Input { get; set; } = new();

        [BindProperty]
        public AddStudentInputModel StudentInput { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class GroupInputModel
        {
            [Required]
            [StringLength(100)]
            public string Name { get; set; } = string.Empty;

            [Required]
            public int Year { get; set; } = DateTime.Now.Year;

            public bool IsActive { get; set; } = true;
        }

        public class AddStudentInputModel
        {
            public int GroupId { get; set; }
            public List<string> SelectedStudents { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            Groups = await _context.Groups
                .OrderBy(g => g.Year)
                .ThenBy(g => g.Name)
                .ToListAsync();

            // Подсчет студентов в каждой группе
            foreach (var group in Groups)
            {
                StudentCounts[group.Id] = await _context.Students
                    .CountAsync(s => s.GroupId == group.Id);
            }

            // Список студентов без группы
            var studentsWithoutGroup = await _context.Students
                .Include(s => s.User)
                .Where(s => s.GroupId == null)
                .ToListAsync();

            ViewData["StudentsWithoutGroup"] = studentsWithoutGroup;
        }

        public async Task<IActionResult> OnPostCreateGroupAsync()
        {
            if (!ModelState.IsValid)
            {
                return RedirectToPage();
            }

            var group = new Group
            {
                Name = Input.Name,
                Year = Input.Year,
                IsActive = Input.IsActive
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync();

            StatusMessage = $"Группа '{group.Name}' создана";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditGroupAsync(int groupId)
        {
            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
            {
                ErrorMessage = "Группа не найдена";
                return RedirectToPage();
            }

            group.Name = Input.Name;
            group.Year = Input.Year;
            group.IsActive = Input.IsActive;

            await _context.SaveChangesAsync();
            StatusMessage = $"Группа '{group.Name}' обновлена";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteGroupAsync(int groupId)
        {
            var group = await _context.Groups
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.Id == groupId);

            if (group == null)
            {
                ErrorMessage = "Группа не найдена";
                return RedirectToPage();
            }

            // Открепляем студентов от группы
            foreach (var student in group.Students)
            {
                student.GroupId = null;
            }

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            StatusMessage = $"Группа удалена";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddStudentsAsync()
        {
            if (StudentInput.SelectedStudents == null || !StudentInput.SelectedStudents.Any())
            {
                ErrorMessage = "Не выбраны студенты для добавления";
                return RedirectToPage();
            }

            var group = await _context.Groups.FindAsync(StudentInput.GroupId);
            if (group == null)
            {
                ErrorMessage = "Группа не найдена";
                return RedirectToPage();
            }

            foreach (var userId in StudentInput.SelectedStudents)
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student != null)
                {
                    student.GroupId = StudentInput.GroupId;
                }
            }

            await _context.SaveChangesAsync();
            StatusMessage = $"Студенты добавлены в группу '{group.Name}'";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveStudentAsync(int studentId, int groupId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student != null)
            {
                student.GroupId = null;
                await _context.SaveChangesAsync();
                StatusMessage = "Студент удален из группы";
            }

            return RedirectToPage();
        }
    }
}