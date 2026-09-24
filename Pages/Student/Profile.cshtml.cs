using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Data;
using ElectronicJournal.Models;
using System.ComponentModel.DataAnnotations;

namespace ElectronicJournal.Pages.Student
{
    public class ProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; } = new();

        [BindProperty]
        public ChangePasswordInputModel PasswordInput { get; set; } = new();

        public Models.Student? CurrentStudent { get; set; }
        public ApplicationUser? CurrentUser { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class ProfileInputModel
        {
            [Required]
            [Display(Name = "Фамилия")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Имя")]
            public string FirstName { get; set; } = string.Empty;

            [Display(Name = "Отчество")]
            public string? MiddleName { get; set; }

            [Phone]
            [Display(Name = "Телефон")]
            public string? PhoneNumber { get; set; }
        }

        public class ChangePasswordInputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Текущий пароль")]
            public string OldPassword { get; set; } = string.Empty;

            [Required]
            [StringLength(100, MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Новый пароль")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Подтверждение пароля")]
            [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            CurrentUser = user;
            CurrentStudent = await _context.Students
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            Input = new ProfileInputModel
            {
                LastName = user.LastName ?? "",
                FirstName = user.FirstName ?? "",
                MiddleName = user.MiddleName,
                PhoneNumber = user.PhoneNumber
            };

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid)
            {
                CurrentUser = user;
                CurrentStudent = await _context.Students
                    .Include(s => s.Group)
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);
                return Page();
            }

            user.LastName = Input.LastName;
            user.FirstName = Input.FirstName;
            user.MiddleName = Input.MiddleName;
            user.PhoneNumber = Input.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = "Профиль успешно обновлен";
            }
            else
            {
                ErrorMessage = "Ошибка при обновлении профиля";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid)
            {
                CurrentUser = user;
                CurrentStudent = await _context.Students
                    .Include(s => s.Group)
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);
                return Page();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(
                user,
                PasswordInput.OldPassword,
                PasswordInput.NewPassword);

            if (changePasswordResult.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                StatusMessage = "Пароль успешно изменен";
            }
            else
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                CurrentUser = user;
                CurrentStudent = await _context.Students
                    .Include(s => s.Group)
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);
                return Page();
            }

            return RedirectToPage();
        }
    }
}