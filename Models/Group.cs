using System.ComponentModel.DataAnnotations;

namespace ElectronicJournal.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Название группы")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Год обучения")]
        public int Year { get; set; } = DateTime.Now.Year;

        [Display(Name = "Активна")]
        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
    }
}