using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Tests.Helpers;

public static class TestDbContext
{
    public static ApplicationDbContext Create(IStudentService? studentService = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Create instance of AgeVerificationService
        IAgeVerificationService ageVerificationService = new AgeVerificationService();

        var context = new ApplicationDbContext(options, ageVerificationService);
        context.Database.EnsureCreated();
        return context;
    }
}
