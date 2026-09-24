using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicJournal.Models
{
    public class Teacher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Display(Name = "Предмет")]
        [StringLength(100)]
        public string? Subject { get; set; }

        [Display(Name = "Кафедра")]
        [StringLength(100)]
        public string? Department { get; set; }

        // Навигационные свойства
        public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}