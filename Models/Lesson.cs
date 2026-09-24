using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace ElectronicJournal.Models
{
    public class Lesson
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Дата занятия")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Display(Name = "Время начала")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "Время окончания")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Тема занятия")]
        public string Topic { get; set; } = string.Empty;

        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Группа")]
        public int GroupId { get; set; }

        [ForeignKey("GroupId")]
        public virtual Group? Group { get; set; }

        [Required]
        [Display(Name = "Преподаватель")]
        public int TeacherId { get; set; }

        [ForeignKey("TeacherId")]
        public virtual Teacher? Teacher { get; set; }

        // Навигационные свойства
        public virtual ICollection<Homework> Homeworks { get; set; } = new List<Homework>();
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public virtual ICollection<LessonAttendance> Attendances { get; set; } = new List<LessonAttendance>();
    }
}