using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicJournal.Models
{
    public class LessonAttendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Студент")]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [Required]
        [Display(Name = "Занятие")]
        public int LessonId { get; set; }

        [ForeignKey("LessonId")]
        public virtual Lesson? Lesson { get; set; }

        [Required]
        [Display(Name = "Присутствовал")]
        public bool IsPresent { get; set; }

        [Display(Name = "Дата отметки")]
        public DateTime MarkedAt { get; set; } = DateTime.Now;
    }
}