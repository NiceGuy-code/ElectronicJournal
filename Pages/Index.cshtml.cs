using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using ElectronicJournal.Models;

namespace ElectronicJournal.Pages
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToPage("/Admin/Users");
                else if (User.IsInRole("Teacher"))
                    return RedirectToPage("/Teacher/MyLessons");
                else if (User.IsInRole("Student"))
                    return RedirectToPage("/Student/Dashboard");
            }

            return RedirectToPage("/Account/Login");
        }
    }
}