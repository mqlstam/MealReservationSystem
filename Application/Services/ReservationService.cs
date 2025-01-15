using System;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services
{
    /// <summary>
    /// Centralized reservation logic for handling package reservations,
    /// picking up, and marking no-shows. 
    /// Reduces duplication in controllers.
    /// </summary>
    public class ReservationService : IReservationService
    {
        private readonly IPackageRepository _packageRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IStudentService _studentService;

        public ReservationService(
            IPackageRepository packageRepository,
            IReservationRepository reservationRepository,
            IStudentService studentService)
        {
            _packageRepository = packageRepository;
            _reservationRepository = reservationRepository;
            _studentService = studentService;
        }

        public async Task<string> ReservePackageAsync(int packageId, string userId)
        {
            // 1. Check if user is valid
            if (string.IsNullOrEmpty(userId))
            {
                return "User not found or not authenticated.";
            }

            // 2. Get student from identity
            var student = await _studentService.GetStudentByIdentityIdAsync(userId);
            if (student == null)
            {
                return "Student record not found.";
            }

            // 3. Get the package
            var package = await _packageRepository.GetByIdAsync(packageId);
            if (package == null)
            {
                return "Package not found.";
            }

            // 4. Already reserved?
            if (package.Reservation != null)
            {
                return "Package already reserved.";
            }

            // 5. Check age restriction
            if (package.IsAdultOnly && !student.IsOfLegalAge)
            {
                return "You must be 18 or older to reserve this package.";
            }

            // 6. Check no-show count
            if (student.NoShowCount >= 2)
            {
                return "You cannot make reservations due to multiple no-shows.";
            }

            // 7. Check if student already has a reservation for the same pickup day
            bool hasReservationSameDay = await _reservationRepository.HasReservationForDateAsync(
                student.IdentityId, package.PickupDateTime.Date);
            if (hasReservationSameDay)
            {
                return "You already have a reservation for this date.";
            }

            // 8. Create the reservation
            var reservation = new Reservation
            {
                PackageId = package.Id,
                StudentNumber = student.StudentNumber,
                ReservationDateTime = DateTime.Now
            };

            await _reservationRepository.AddAsync(reservation);
            return "Package reserved successfully!";
        }

        public async Task MarkAsPickedUpAsync(int packageId)
        {
            var package = await _packageRepository.GetByIdAsync(packageId);
            if (package?.Reservation == null) return;

            package.Reservation.IsPickedUp = true;
            await _packageRepository.UpdateAsync(package);
            // Optionally, you could do other logic here 
            // (e.g., awarding student points for picking up on time, etc.).
        }

        public async Task MarkAsNoShowAsync(int packageId)
        {
            var package = await _packageRepository.GetByIdAsync(packageId);
            if (package?.Reservation == null) return;

            package.Reservation.IsNoShow = true;
            await _packageRepository.UpdateAsync(package);

            // Increment student's no-show count
            var student = await _studentService.GetStudentByNumberAsync(package.Reservation.StudentNumber);
            if (student != null)
            {
                await _studentService.UpdateNoShowCountAsync(
                    package.Reservation.StudentNumber,
                    student.NoShowCount + 1);
            }
        }
    }
}
