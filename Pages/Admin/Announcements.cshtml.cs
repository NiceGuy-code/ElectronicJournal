using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class AnnouncementsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnnouncementsModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [BindProperty]
        public AnnouncementInputModel Input { get; set; } = new();

        public List<Announcement> Announcements { get; set; } = new();
        public SelectList? GroupsSelectList { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public class AnnouncementInputModel
        {
            [Required]
            [StringLength(200)]
            [Display(Name = "Заголовок")]
            public string Title { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Текст объявления")]
            public string Content { get; set; } = string.Empty;

            [Display(Name = "Для всех")]
            public bool IsForAll { get; set; } = true;

            [Display(Name = "Для группы")]
            public int? GroupId { get; set; }
        }

        public async Task OnGetAsync()
        {
            Announcements = await _context.Announcements
                .Include(a => a.Author)
                .Include(a => a.Group)
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            GroupsSelectList = new SelectList(
                await _context.Groups.Where(g => g.IsActive).ToListAsync(),
                "Id", "Name");
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);

            var announcement = new Announcement
            {
                Title = Input.Title,
                Content = Input.Content,
                AuthorId = user.Id,
                IsForAll = Input.IsForAll,
                GroupId = Input.IsForAll ? null : Input.GroupId,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            StatusMessage = "Объявление создано";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                announcement.IsActive = false;
                await _context.SaveChangesAsync();
                StatusMessage = "Объявление удалено";
            }

            return RedirectToPage();
        }
    }
}