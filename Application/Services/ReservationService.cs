using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class ReservationService : IReservationService
{
    private readonly IPackageRepository _packageRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IStudentService _studentService;
    private readonly INoShowService _noShowService;
    private readonly IAgeVerificationService _ageVerificationService;

    public ReservationService(
        IPackageRepository packageRepository,
        IReservationRepository reservationRepository,
        IStudentService studentService,
        INoShowService noShowService,
        IAgeVerificationService ageVerificationService)
    {
        _packageRepository = packageRepository;
        _reservationRepository = reservationRepository;
        _studentService = studentService;
        _noShowService = noShowService;
        _ageVerificationService = ageVerificationService;
    }

    public async Task<string> ReservePackageAsync(int packageId, string userId)
    {
        // Get the student from identity
        var student = await _studentService.GetStudentByIdentityIdAsync(userId);
        if (student == null)
            return "Student record not found";

        // Get the package
        var package = await _packageRepository.GetByIdAsync(packageId);
        if (package == null)
            return "Package not found";

        // Check if already reserved
        if (package.Reservation != null)
            return "Package already reserved";

        // Check if pickup time has passed
        if (package.PickupDateTime <= DateTime.Now)
            return "Pickup time has passed";

        // Check if reservation deadline has passed
        if (package.LastReservationDateTime <= DateTime.Now)
            return "Reservation time has passed";

        // Check no-show limit
        if (!await _noShowService.CanStudentReserveAsync(userId))
            return "You cannot make reservations due to multiple no-shows";

        // Check age restriction
        if (!_ageVerificationService.IsStudentEligibleForPackage(student, package))
            return "You must be 18 or older to reserve this package";

        // Check existing reservation for the date
        if (await _reservationRepository.HasReservationForDateAsync(userId, package.PickupDateTime.Date))
            return "You already have a reservation for this date";

        try
        {
            // Create reservation
            var reservation = new Reservation
            {
                PackageId = package.Id,
                StudentNumber = student.StudentNumber,
                ReservationDateTime = DateTime.Now
            };

            await _reservationRepository.AddAsync(reservation);
            return "Package reserved successfully!";
        }
        catch (DbUpdateConcurrencyException)
        {
            // Handle concurrent reservations
            return "Package already reserved";
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task MarkAsPickedUpAsync(int packageId)
    {
        var package = await _packageRepository.GetByIdAsync(packageId);
        if (package?.Reservation == null)
            return;

        package.Reservation.IsPickedUp = true;
        await _packageRepository.UpdateAsync(package);
    }

    public async Task MarkAsNoShowAsync(int packageId)
    {
        var package = await _packageRepository.GetByIdAsync(packageId);
        if (package?.Reservation == null)
            return;

        await _noShowService.ProcessNoShowAsync(package.Reservation);
    }
}
