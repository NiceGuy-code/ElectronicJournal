using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Data;

namespace ElectronicJournal.Controllers
{
    [Authorize(Roles = "Teacher")]
    [Route("api/teacher")]
    [ApiController]
    public class TeacherApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TeacherApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("grade-submission")]
        public async Task<IActionResult> GradeSubmission([FromForm] int submissionId, [FromForm] int lessonId, [FromForm] int grade, [FromForm] string? teacherComment)
        {
            if (grade < 1 || grade > 5)
            {
                return BadRequest(new { success = false, message = "Оценка должна быть от 1 до 5" });
            }

            var submission = await _context.StudentHomeworks
                .Include(sh => sh.Student).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(sh => sh.Id == submissionId);

            if (submission == null)
            {
                return NotFound(new { success = false, message = "Работа не найдена" });
            }

            submission.Grade = grade;
            submission.TeacherComment = teacherComment;
            submission.IsChecked = true;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Работа оценена на {grade}" });
        }
    }
}