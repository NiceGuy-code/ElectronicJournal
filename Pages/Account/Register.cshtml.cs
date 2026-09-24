using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using ElectronicJournal.Data;
using ElectronicJournal.Models;

namespace ElectronicJournal.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Поле Фамилия обязательно")]
            [Display(Name = "Фамилия")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Поле Имя обязательно")]
            [Display(Name = "Имя")]
            public string FirstName { get; set; } = string.Empty;

            [Display(Name = "Отчество")]
            public string? MiddleName { get; set; }

            [Required(ErrorMessage = "Поле Email обязательно")]
            [EmailAddress(ErrorMessage = "Некорректный Email")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Поле Пароль обязательно")]
            [StringLength(100, ErrorMessage = "Пароль должен быть минимум {2} символов.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Подтверждение пароля")]
            [Compare("Password", ErrorMessage = "Пароли не совпадают.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    MiddleName = Input.MiddleName
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Пользователь создал новую учетную запись.");

                    // По умолчанию даем роль Student
                    await _userManager.AddToRoleAsync(user, "Student");

                    // Создаем запись студента
                    var student = new Models.Student
                    {
                        UserId = user.Id
                    };

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    // Автоматически входим после регистрации
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return LocalRedirect("/Student/Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }
    }
}