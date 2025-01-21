using Application.Common.Interfaces;
using HotChocolate;
using HotChocolate.Types;

namespace WebApi.GraphQL;

[ExtendObjectType("Query")]
public class MealReservationQuery
{
    private readonly IPackageRepository _packageRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<MealReservationQuery> _logger;

    public MealReservationQuery(
        IPackageRepository packageRepository,
        IReservationRepository reservationRepository,
        ILogger<MealReservationQuery> logger)
    {
        _packageRepository = packageRepository;
        _reservationRepository = reservationRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Domain.Entities.Package>> GetAvailablePackages()
    {
        try
        {
            var packages = await _packageRepository.GetAvailablePackagesAsync();
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
            var package = await _packageRepository.GetByIdAsync(id);
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
            var cafeteriaLocation = Enum.Parse<Domain.Enums.CafeteriaLocation>(location);
            var packages = await _packageRepository.GetByLocationAsync(cafeteriaLocation);
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
            var reservations = await _reservationRepository.GetByStudentIdAsync(studentId);
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
