using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Data;
using ElectronicJournal.Models;
using Microsoft.AspNetCore.Identity;
using System.IO.Compression;

namespace ElectronicJournal.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public FileController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSubmissions(int lessonId)
        {
            var user = await _userManager.GetUserAsync(User);
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);

            // Проверяем, что занятие принадлежит этому учителю
            var lesson = await _context.Lessons
                .Include(l => l.Homeworks)
                .FirstOrDefaultAsync(l => l.Id == lessonId && l.TeacherId == teacher.Id);

            if (lesson == null)
                return NotFound();

            // Получаем все сданные работы по этому занятию
            var homeworkIds = lesson.Homeworks.Select(h => h.Id).ToList();
            var submissions = await _context.StudentHomeworks
                .Include(sh => sh.Student)
                    .ThenInclude(s => s.User)
                .Include(sh => sh.Homework)
                .Where(sh => homeworkIds.Contains(sh.HomeworkId) && !string.IsNullOrEmpty(sh.FilePath))
                .ToListAsync();

            if (!submissions.Any())
                return NotFound("Нет сданных работ");

            // Создаем ZIP-архив
            var zipName = $"Работы_студентов_{lesson.Topic}_{DateTime.Now:yyyyMMdd}.zip";
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var submission in submissions)
                    {
                        var filePath = Path.Combine(_environment.WebRootPath, submission.FilePath.TrimStart('/'));

                        if (System.IO.File.Exists(filePath))
                        {
                            var studentName = submission.Student?.User?.FullName ?? "Неизвестный";
                            var homeworkDesc = submission.Homework?.Description ?? "ДЗ";

                            // Создаем понятное имя файла в архиве
                            var entryName = $"{studentName}/{homeworkDesc}/{Path.GetFileName(filePath)}";
                            var entry = archive.CreateEntry(entryName);

                            using (var entryStream = entry.Open())
                            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                            {
                                await fileStream.CopyToAsync(entryStream);
                            }
                        }
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                return File(memoryStream.ToArray(), "application/zip", zipName);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(string filePath)
        {
            var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            var fileName = Path.GetFileName(fullPath);

            return File(fileBytes, "application/octet-stream", fileName);
        }
    }
}