using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicJournal.Models
{
    public class Grade
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

        [Display(Name = "Оценка за занятие")]
        [Range(1, 5)]
        public int? LessonGrade { get; set; }

        [Display(Name = "Оценка за ДЗ")]
        [Range(1, 5)]
        public int? HomeworkGrade { get; set; }

        [Display(Name = "Не аттестован")]
        public bool IsNotCertified { get; set; } = false;

        [Display(Name = "Примечание")]
        [StringLength(500)]
        public string? Comment { get; set; }

        [Display(Name = "Дата выставления")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}