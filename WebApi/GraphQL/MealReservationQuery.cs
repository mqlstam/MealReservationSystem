// WebApi/GraphQL/MealReservationQuery.cs
using Application.Common.Interfaces;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace WebApi.GraphQL;

[ExtendObjectType("Query")]
public class MealReservationQuery
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MealReservationQuery> _logger;

    public MealReservationQuery(
        IServiceScopeFactory scopeFactory,
        ILogger<MealReservationQuery> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<Domain.Entities.Package>> GetAvailablePackages()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var packageRepository = scope.ServiceProvider.GetRequiredService<IPackageRepository>();
            
            var packages = await packageRepository.GetAvailablePackagesAsync();
            _logger.LogInformation("Retrieved {Count} available packages", packages.Count());
            return packages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available packages");
            throw;
        }
    }

    public async Task<Domain.Entities.Package?> GetPackage(int id)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var packageRepository = scope.ServiceProvider.GetRequiredService<IPackageRepository>();
            
            var package = await packageRepository.GetByIdAsync(id);
            _logger.LogInformation("Retrieved package with ID {Id}", id);
            return package;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving package with ID {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Domain.Entities.Package>> GetPackagesByLocation(string location)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var packageRepository = scope.ServiceProvider.GetRequiredService<IPackageRepository>();
            
            var cafeteriaLocation = Enum.Parse<Domain.Enums.CafeteriaLocation>(location);
            var packages = await packageRepository.GetByLocationAsync(cafeteriaLocation);
            _logger.LogInformation("Retrieved {Count} packages for location {Location}", 
                packages.Count(), location);
            return packages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving packages for location {Location}", location);
            throw;
        }
    }

    public async Task<IEnumerable<Domain.Entities.Reservation>> GetReservations(string studentId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var reservationRepository = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
            
            var reservations = await reservationRepository.GetByStudentIdAsync(studentId);
            _logger.LogInformation("Retrieved {Count} reservations for student {StudentId}", 
                reservations.Count(), studentId);
            return reservations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservations for student {StudentId}", studentId);
            throw;
        }
    }
}