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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Database Contexts
        services.AddDbContext<ApplicationDbContext>((provider, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.CommandTimeout(5);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 1,
                        maxRetryDelay: TimeSpan.FromSeconds(1),
                        errorNumbersToAdd: null);
                });
        });

        services.AddDbContext<ApplicationIdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("IdentityConnection"),
                sqlOptions =>
                {
                    sqlOptions.CommandTimeout(5);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 1,
                        maxRetryDelay: TimeSpan.FromSeconds(1),
                        errorNumbersToAdd: null);
                }));

        // Configure Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationIdentityDbContext>()
            .AddDefaultTokenProviders();

        // Register Core Services with explicit lifetimes
        services.AddScoped<IAgeVerificationService, AgeVerificationService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IPackageViewService, PackageViewService>();
        services.AddScoped<IReservationService, ReservationService>();

        // Register Infrastructure Services/Repositories
        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<ICafeteriaRepository, CafeteriaRepository>();

        // Register authentication and authorization services
        services.AddAuthentication()
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
            });
            
        services.AddAuthorizationCore();

        return services;
    }
}
