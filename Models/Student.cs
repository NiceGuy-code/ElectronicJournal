using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace ElectronicJournal.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Display(Name = "Группа")]
        public int? GroupId { get; set; }

        [ForeignKey("GroupId")]
        public virtual Group? Group { get; set; }

        // Навигационные свойства
        public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();
        public virtual ICollection<StudentHomework> StudentHomeworks { get; set; } = new List<StudentHomework>();
        public virtual ICollection<LessonAttendance> Attendances { get; set; } = new List<LessonAttendance>();
    }
}