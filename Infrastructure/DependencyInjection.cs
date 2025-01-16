using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.Services;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register AgeVerificationService first
        services.AddScoped<IAgeVerificationService, AgeVerificationService>();

        // Add Identity DbContext
        services.AddDbContext<ApplicationIdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("IdentityConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationIdentityDbContext).Assembly.FullName)));

        // Add Application DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        // Configure Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationIdentityDbContext>();

        // Register repositories
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<ICafeteriaRepository, CafeteriaRepository>();
        
        // Register DbContext interface
        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());

        // Register services
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IPackageViewService, PackageViewService>();

        return services;
    }
}
