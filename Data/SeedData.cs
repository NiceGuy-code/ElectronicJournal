using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ElectronicJournal.Models;

namespace ElectronicJournal.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Создаем роли, если они не существуют
            string[] roleNames = { "Admin", "Teacher", "Student" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Создаем администратора
            var adminEmail = "admin@journal.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Администратор",
                    LastName = "Системы",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                // Если админ уже есть, проверяем что у него есть роль Admin
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Создаем тестового учителя
            var teacherEmail = "teacher@journal.com";
            var teacherUser = await userManager.FindByEmailAsync(teacherEmail);

            if (teacherUser == null)
            {
                teacherUser = new ApplicationUser
                {
                    UserName = teacherEmail,
                    Email = teacherEmail,
                    FirstName = "Иван",
                    LastName = "Петров",
                    MiddleName = "Сергеевич",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(teacherUser, "Teacher123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(teacherUser, "Teacher");

                    // Создаем запись учителя
                    var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
                    var teacher = new Teacher
                    {
                        UserId = teacherUser.Id,
                        Subject = "Математика",
                        Department = "Естественных наук"
                    };

                    context.Teachers.Add(teacher);
                    await context.SaveChangesAsync();
                }
            }

            // Создаем тестового студента
            var studentEmail = "student@journal.com";
            var studentUser = await userManager.FindByEmailAsync(studentEmail);

            if (studentUser == null)
            {
                studentUser = new ApplicationUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    FirstName = "Анна",
                    LastName = "Смирнова",
                    MiddleName = "Ивановна",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(studentUser, "Student123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(studentUser, "Student");

                    // Создаем запись студента
                    var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

                    // Убеждаемся что есть группа
                    var group = await context.Groups.FirstOrDefaultAsync();
                    if (group == null)
                    {
                        group = new Group
                        {
                            Name = "Группа А-101",
                            Year = DateTime.Now.Year,
                            IsActive = true
                        };
                        context.Groups.Add(group);
                        await context.SaveChangesAsync();
                    }

                    var student = new Student
                    {
                        UserId = studentUser.Id,
                        GroupId = group.Id
                    };

                    context.Students.Add(student);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}