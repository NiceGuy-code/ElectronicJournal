using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Data;
using ElectronicJournal.Models;

namespace ElectronicJournal.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UsersModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public List<UserViewModel> Users { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public List<string> Roles { get; set; } = new();
            public bool IsStudent { get; set; }
            public bool IsTeacher { get; set; }
            public int? StudentId { get; set; }
            public int? TeacherId { get; set; }
        }

        public async Task OnGetAsync()
        {
            var users = await _userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);

                Users.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    FullName = user.FullName,
                    Roles = roles.ToList(),
                    IsStudent = student != null,
                    IsTeacher = teacher != null,
                    StudentId = student?.Id,
                    TeacherId = teacher?.Id
                });
            }
        }

        public async Task<IActionResult> OnPostChangeRoleAsync(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ErrorMessage = "Пользователь не найден";
                return RedirectToPage();
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Удаляем текущие роли
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Добавляем новую роль
            await _userManager.AddToRoleAsync(user, newRole);

            // Обновляем соответствующие записи
            if (newRole == "Teacher")
            {
                // Создаем запись учителя, если нет
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
                if (teacher == null)
                {
                    teacher = new Models.Teacher
                    {
                        UserId = userId,
                        Subject = "Не указан"
                    };
                    _context.Teachers.Add(teacher);
                }

                // Удаляем запись студента, если есть
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student != null)
                {
                    _context.Students.Remove(student);
                }
            }
            else if (newRole == "Student")
            {
                // Создаем запись студента, если нет
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student == null)
                {
                    student = new Models.Student
                    {
                        UserId = userId
                    };
                    _context.Students.Add(student);
                }

                // Удаляем запись учителя, если есть
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == userId);
                if (teacher != null)
                {
                    _context.Teachers.Remove(teacher);
                }
            }

            await _context.SaveChangesAsync();

            StatusMessage = $"Роль пользователя {user.Email} изменена на {newRole}";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ErrorMessage = "Пользователь не найден";
                return RedirectToPage();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var newPassword = "Reset123!";
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                StatusMessage = $"Пароль пользователя {user.Email} сброшен. Новый пароль: {newPassword}";
            }
            else
            {
                ErrorMessage = "Ошибка при сбросе пароля";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ErrorMessage = "Пользователь не найден";
                return RedirectToPage();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = $"Пользователь {user.Email} удален";
            }
            else
            {
                ErrorMessage = "Ошибка при удалении пользователя";
            }

            return RedirectToPage();
        }
    }
}