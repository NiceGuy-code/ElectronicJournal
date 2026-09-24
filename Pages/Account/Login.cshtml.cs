using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using ElectronicJournal.Models;

namespace ElectronicJournal.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Введите Email")]
            [EmailAddress(ErrorMessage = "Некорректный Email")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Введите пароль")]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Запомнить меня")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Очищаем внешний cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    Input.Email,
                    Input.Password,
                    Input.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Пользователь вошел в систему.");

                    // Перенаправляем в зависимости от роли
                    var user = await _userManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                            return LocalRedirect("/Admin/Users");
                        else if (await _userManager.IsInRoleAsync(user, "Teacher"))
                            return LocalRedirect("/Teacher/MyLessons");
                        else if (await _userManager.IsInRoleAsync(user, "Student"))
                            return LocalRedirect("/Student/Dashboard");
                    }

                    return LocalRedirect(returnUrl);
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Учетная запись пользователя заблокирована.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Неверная попытка входа.");
                    return Page();
                }
            }

            return Page();
        }
    }
}