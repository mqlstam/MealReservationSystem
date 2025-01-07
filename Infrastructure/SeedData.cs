using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure;

public static class SeedData
{
    public static async Task Initialize(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // Seed Roles
        string[] roleNames = { "Admin", "Student", "CafeteriaEmployee" };
        foreach (var roleName in roleNames)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }

        // Seed Admin User
        var adminEmail = "admin@avans.nl";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed Demo Cafeteria Employee
        var employeeEmail = "employee@avans.nl";
        var employeeUser = await userManager.FindByEmailAsync(employeeEmail);
        if (employeeUser == null)
        {
            employeeUser = new ApplicationUser
            {
                UserName = employeeEmail,
                Email = employeeEmail,
                FirstName = "Demo",
                LastName = "Employee",
                EmailConfirmed = true,
                EmployeeNumber = "E12345",
                CafeteriaLocation = CafeteriaLocation.LA.ToString()
            };

            var result = await userManager.CreateAsync(employeeUser, "Employee123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(employeeUser, "CafeteriaEmployee");
            }
        }

        // Seed Demo Student
        var studentEmail = "student@avans.nl";
        var studentUser = await userManager.FindByEmailAsync(studentEmail);
        if (studentUser == null)
        {
            studentUser = new ApplicationUser
            {
                UserName = studentEmail,
                Email = studentEmail,
                FirstName = "Demo",
                LastName = "Student",
                EmailConfirmed = true,
                StudentNumber = "2123456",
                StudyCity = City.Breda.ToString(),
                DateOfBirth = new DateTime(2000, 1, 1)
            };

            var result = await userManager.CreateAsync(studentUser, "Student123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(studentUser, "Student");
            }
        }
    }
}
