using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectronicJournal.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Заголовок")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Текст объявления")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Дата публикации")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Автор")]
        public string AuthorId { get; set; } = string.Empty;

        [ForeignKey("AuthorId")]
        public virtual ApplicationUser? Author { get; set; }

        [Display(Name = "Для группы")]
        public int? GroupId { get; set; }

        [ForeignKey("GroupId")]
        public virtual Group? Group { get; set; }

        [Display(Name = "Для всех")]
        public bool IsForAll { get; set; } = true;

        [Display(Name = "Активно")]
        public bool IsActive { get; set; } = true;
    }
}