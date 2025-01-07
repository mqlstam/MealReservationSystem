using Application.Common.Interfaces;
using HotChocolate;
using HotChocolate.Types;

namespace WebApi.GraphQL;

[ExtendObjectType("Query")]
public class MealReservationQuery
{
    private readonly IPackageRepository _packageRepository;
    private readonly IReservationRepository _reservationRepository;

    public MealReservationQuery(
        IPackageRepository packageRepository,
        IReservationRepository reservationRepository)
    {
        _packageRepository = packageRepository;
        _reservationRepository = reservationRepository;
    }

    public async Task<IEnumerable<Domain.Entities.Package>> GetAvailablePackages()
    {
        return await _packageRepository.GetAvailablePackagesAsync();
    }

    public async Task<Domain.Entities.Package?> GetPackage(int id)
    {
        return await _packageRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Domain.Entities.Package>> GetPackagesByLocation(string location)
    {
        var cafeteriaLocation = Enum.Parse<Domain.Enums.CafeteriaLocation>(location);
        return await _packageRepository.GetByLocationAsync(cafeteriaLocation);
    }

    public async Task<IEnumerable<Domain.Entities.Reservation>> GetReservations(string studentId)
    {
        return await _reservationRepository.GetByStudentIdAsync(studentId);
    }
}
