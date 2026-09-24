using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicJournal.Models
{
    public class StudentHomework
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Студент")]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [Required]
        [Display(Name = "Задание")]
        public int HomeworkId { get; set; }

        [ForeignKey("HomeworkId")]
        public virtual Homework? Homework { get; set; }

        [Display(Name = "Файл с решением")]
        [StringLength(500)]
        public string? FilePath { get; set; }

        [Display(Name = "Комментарий студента")]
        public string? StudentComment { get; set; }

        [Display(Name = "Дата сдачи")]
        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        [Display(Name = "Оценка")]
        public int? Grade { get; set; }

        [Display(Name = "Комментарий преподавателя")]
        public string? TeacherComment { get; set; }

        [Display(Name = "Проверено")]
        public bool IsChecked { get; set; } = false;
    }
}