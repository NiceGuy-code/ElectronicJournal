using Microsoft.AspNetCore.Identity;

namespace ElectronicJournal.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Дополнительные поля пользователя
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }

        // Навигационные свойства
        public virtual Student? Student { get; set; }
        public virtual Teacher? Teacher { get; set; }

        // Удобное свойство для отображения полного имени
        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    }
}