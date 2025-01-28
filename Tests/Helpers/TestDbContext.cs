using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Application.Common.Interfaces.Services;

namespace Tests.Helpers;

public static class TestDbContext
{
    public static ApplicationDbContext Create()
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
