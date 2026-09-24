using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Models;
using Microsoft.AspNetCore.Identity;

namespace ElectronicJournal.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Таблицы базы данных
        public DbSet<Group> Groups { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Homework> Homeworks { get; set; }
        public DbSet<StudentHomework> StudentHomeworks { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<LessonAttendance> LessonAttendances { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка связей и ограничений

            // Student - User (один к одному)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Teacher - User (один к одному)
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne(u => u.Teacher)
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Student - Group
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Group)
                .WithMany(g => g.Students)
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            // Lesson - Group
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Group)
                .WithMany(g => g.Lessons)
                .HasForeignKey(l => l.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Lesson - Teacher
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Teacher)
                .WithMany(t => t.Lessons)
                .HasForeignKey(l => l.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            // Homework - Lesson
            modelBuilder.Entity<Homework>()
                .HasOne(h => h.Lesson)
                .WithMany(l => l.Homeworks)
                .HasForeignKey(h => h.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            // StudentHomework - Student и Homework
            modelBuilder.Entity<StudentHomework>()
                .HasOne(sh => sh.Student)
                .WithMany(s => s.StudentHomeworks)
                .HasForeignKey(sh => sh.StudentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<StudentHomework>()
                .HasOne(sh => sh.Homework)
                .WithMany(h => h.StudentHomeworks)
                .HasForeignKey(sh => sh.HomeworkId)
                .OnDelete(DeleteBehavior.Cascade);

            // Уникальность Student-Homework
            modelBuilder.Entity<StudentHomework>()
                .HasIndex(sh => new { sh.StudentId, sh.HomeworkId })
                .IsUnique();

            // Grade - Student и Lesson
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Lesson)
                .WithMany(l => l.Grades)
                .HasForeignKey(g => g.LessonId)
                .OnDelete(DeleteBehavior.NoAction);

            // Уникальность Student-Lesson в Grades
            modelBuilder.Entity<Grade>()
                .HasIndex(g => new { g.StudentId, g.LessonId })
                .IsUnique();

            // LessonAttendance - Student и Lesson
            modelBuilder.Entity<LessonAttendance>()
                .HasOne(la => la.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(la => la.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LessonAttendance>()
                .HasOne(la => la.Lesson)
                .WithMany(l => l.Attendances)
                .HasForeignKey(la => la.LessonId)
                .OnDelete(DeleteBehavior.NoAction);

            // Уникальность Student-Lesson в Attendances
            modelBuilder.Entity<LessonAttendance>()
                .HasIndex(la => new { la.StudentId, la.LessonId })
                .IsUnique();

            // Announcement - Author
            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.Author)
                .WithMany()
                .HasForeignKey(a => a.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Announcement - Group
            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.Group)
                .WithMany(g => g.Announcements)
                .HasForeignKey(a => a.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}