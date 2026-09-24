using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicJournal.Models
{
    public class Homework
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Занятие")]
        public int LessonId { get; set; }

        [ForeignKey("LessonId")]
        public virtual Lesson? Lesson { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Описание задания")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Файл задания")]
        [StringLength(500)]
        public string? FilePath { get; set; }

        [Display(Name = "Ссылка на ресурс")]
        [StringLength(500)]
        public string? Link { get; set; }

        [Display(Name = "Срок сдачи")]
        [DataType(DataType.DateTime)]
        public DateTime? Deadline { get; set; }

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Навигационные свойства
        public virtual ICollection<StudentHomework> StudentHomeworks { get; set; } = new List<StudentHomework>();
    }
}