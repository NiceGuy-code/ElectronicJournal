using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Data;
using ElectronicJournal.Models;
using Microsoft.AspNetCore.Identity;

namespace ElectronicJournal.Pages.Account
{
    public class AnnouncementsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnnouncementsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Announcement> Announcements { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? PageNumber { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login");

            CurrentPage = PageNumber ?? 1;
            var pageSize = 10;

            IQueryable<Announcement> query;

            if (User.IsInRole("Admin"))
            {
                // Админ видит все объявления
                query = _context.Announcements
                    .Include(a => a.Author)
                    .Include(a => a.Group)
                    .Where(a => a.IsActive);
            }
            else if (User.IsInRole("Student"))
            {
                // Студент видит общие и своей группы
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId == user.Id);

                query = _context.Announcements
                    .Include(a => a.Author)
                    .Include(a => a.Group)
                    .Where(a => a.IsActive &&
                        (a.IsForAll || a.GroupId == student.GroupId));
            }
            else if (User.IsInRole("Teacher"))
            {
                // Учитель видит общие и свои объявления
                var teacher = await _context.Teachers
                    .FirstOrDefaultAsync(t => t.UserId == user.Id);

                query = _context.Announcements
                    .Include(a => a.Author)
                    .Include(a => a.Group)
                    .Where(a => a.IsActive &&
                        (a.IsForAll || a.AuthorId == user.Id));
            }
            else
            {
                query = _context.Announcements
                    .Include(a => a.Author)
                    .Where(a => a.IsActive && a.IsForAll);
            }

            var totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            Announcements = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }
    }
}