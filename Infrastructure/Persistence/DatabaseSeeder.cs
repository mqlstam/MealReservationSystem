using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedDatabase(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // Create roles if they don't exist
        string[] roles = { "Admin", "Student", "CafeteriaEmployee" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }

        // Seed Cafeterias if they don't exist
        if (!await context.Cafeterias.AnyAsync())
        {
            var cafeterias = new List<Cafeteria>
            {
                new() { City = City.Breda, Location = CafeteriaLocation.LA, OffersHotMeals = true },
                new() { City = City.Breda, Location = CafeteriaLocation.LD, OffersHotMeals = true },
                new() { City = City.Breda, Location = CafeteriaLocation.HA, OffersHotMeals = false },
                new() { City = City.Breda, Location = CafeteriaLocation.HB, OffersHotMeals = false },
                new() { City = City.DenBosch, Location = CafeteriaLocation.DB, OffersHotMeals = true },
                new() { City = City.Tilburg, Location = CafeteriaLocation.TB, OffersHotMeals = true }
            };

            context.Cafeterias.AddRange(cafeterias);
            await context.SaveChangesAsync();
        }

        // Create demo accounts if they don't exist
        await CreateDemoAccountsAsync(userManager, context);
    }

    private static async Task CreateDemoAccountsAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        // Demo Student
        var studentEmail = "student@test.com";
        if (await userManager.FindByEmailAsync(studentEmail) == null)
        {
            var student = new ApplicationUser
            {
                UserName = studentEmail,
                Email = studentEmail,
                FirstName = "Demo",
                LastName = "Student",
                StudentNumber = "2024001",
                DateOfBirth = new DateTime(2000, 1, 1),
                StudyCity = City.Breda.ToString(),
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(student, "Test123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(student, "Student");

                // Create corresponding student record in main database
                if (!await context.Students.AnyAsync(s => s.StudentNumber == student.StudentNumber))
                {
                    var studentRecord = new Student
                    {
                        StudentNumber = student.StudentNumber,
                        Email = student.Email,
                        FirstName = student.FirstName,
                        LastName = student.LastName,
                        DateOfBirth = student.DateOfBirth!.Value,
                        StudyCity = City.Breda,
                        IdentityId = student.Id,
                        NoShowCount = 0
                    };

                    context.Students.Add(studentRecord);
                    await context.SaveChangesAsync();
                }
            }
        }

        // Demo Employee
        var employeeEmail = "employee@test.com";
        if (await userManager.FindByEmailAsync(employeeEmail) == null)
        {
            var employee = new ApplicationUser
            {
                UserName = employeeEmail,
                Email = employeeEmail,
                FirstName = "Demo",
                LastName = "Employee",
                EmployeeNumber = "E2024001",
                CafeteriaLocation = CafeteriaLocation.LA.ToString(),
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(employee, "Test123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(employee, "CafeteriaEmployee");
            }
        }
    }
}
