using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.Services;
using Application.Services.NoShow;
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
        services.AddDatabaseServices(configuration);
        services.AddIdentityServices();
        services.AddCoreServices();
        services.AddInfrastructureAuth();

        return services;
    }

    private static IServiceCollection AddDatabaseServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
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

        return services;
    }

    private static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationIdentityDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    private static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Register Core Services with explicit lifetimes
        services.AddScoped<IAgeVerificationService, AgeVerificationService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IPackageViewService, PackageViewService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<INoShowService, NoShowService>();

        // Register Infrastructure Services/Repositories
        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<ICafeteriaRepository, CafeteriaRepository>();

        return services;
    }
    
    private static IServiceCollection AddInfrastructureAuth(this IServiceCollection services)
    {
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
