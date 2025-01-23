using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.Services.PackageManagement;
using Application.Services.PackageManagement.DTOs;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Application.Services.PackageManagement
{
    public class PackageManagementService : IPackageManagementService
    {
        private readonly IPackageRepository _packageRepository;
        private readonly ICafeteriaRepository _cafeteriaRepository;
        private readonly IStudentService _studentService;
        private readonly ICurrentUserService _currentUserService;

        public PackageManagementService(
            IPackageRepository packageRepository,
            ICafeteriaRepository cafeteriaRepository,
            IStudentService studentService,
            ICurrentUserService currentUserService)
        {
            _packageRepository = packageRepository;
            _cafeteriaRepository = cafeteriaRepository;
            _studentService = studentService;
            _currentUserService = currentUserService;
        }

        public async Task<PackageListDto> GetPackageListAsync(
            string employeeId,
            bool showOnlyMyCafeteria,
            City? cityFilter,
            MealType? typeFilter,
            decimal? maxPrice,
            bool showExpired)
        {
            // Retrieve employee's cafeteria location
            var employeeCafeteriaLoc = await _currentUserService.GetCafeteriaLocationAsync(employeeId);
            if (string.IsNullOrEmpty(employeeCafeteriaLoc))
            {
                // Handle error appropriately
                return new PackageListDto { Packages = new List<PackageManagementDto>() };
            }

            if (!Enum.TryParse<CafeteriaLocation>(employeeCafeteriaLoc, true, out var location))
            {
                // Handle parsing error
                return new PackageListDto { Packages = new List<PackageManagementDto>() };
            }

            // Retrieve cafeteria to get the city
            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(location);
            if (cafeteria == null)
            {
                // Handle cafeteria not found
                return new PackageListDto { Packages = new List<PackageManagementDto>() };
            }

            var packages = await _packageRepository.GetAllAsync();
            var result = new List<PackageManagementDto>();

            foreach (var package in packages)
            {
                // Possibly get the reserved student's name
                string? reservedBy = null;
                if (package.Reservation != null)
                {
                    var stud = await _studentService.GetStudentByNumberAsync(package.Reservation.StudentNumber);
                    if (stud != null)
                        reservedBy = $"{stud.FirstName} {stud.LastName}";
                }

                var dto = new PackageManagementDto
                {
                    Id = package.Id,
                    Name = package.Name,
                    City = cafeteria.City, // Set based on cafeteria's city
                    CafeteriaLocation = package.CafeteriaLocation,
                    PickupDateTime = package.PickupDateTime,
                    LastReservationDateTime = package.LastReservationDateTime,
                    IsAdultOnly = package.IsAdultOnly,
                    Price = package.Price,
                    MealType = package.MealType,
                    Products = package.Products.Select(p => p.Name).ToList(),
                    IsReserved = (package.Reservation != null),
                    IsPickedUp = package.Reservation?.IsPickedUp ?? false,
                    IsNoShow = package.Reservation?.IsNoShow ?? false,
                    ReservedBy = reservedBy
                };

                result.Add(dto);
            }

            if (showOnlyMyCafeteria)
            {
                result = result.Where(x => x.CafeteriaLocation == location).ToList();
            }

            // Additional filters
            var query = result.AsQueryable();

            if (cityFilter.HasValue)
                query = query.Where(x => x.City == cityFilter.Value);

            if (typeFilter.HasValue)
                query = query.Where(x => x.MealType == typeFilter.Value);

            if (maxPrice.HasValue)
                query = query.Where(x => x.Price <= maxPrice.Value);

            if (!showExpired)
                query = query.Where(x => !x.IsExpired);

            var finalList = query.OrderBy(x => x.PickupDateTime).ToList();

            return new PackageListDto
            {
                Packages = finalList,
                CityFilter = cityFilter,
                TypeFilter = typeFilter,
                MaxPriceFilter = maxPrice,
                ShowExpired = showExpired
            };
        }

        public async Task<(bool Success, string ErrorMessage)> CreatePackageAsync(
            string employeeId,
            CreatePackageDto dto)
        {
            // Retrieve employee's cafeteria location
            var employeeCafeteriaLoc = await _currentUserService.GetCafeteriaLocationAsync(employeeId);
            if (string.IsNullOrEmpty(employeeCafeteriaLoc))
                return (false, "User not found or cafeteria location not set");

            if (!Enum.TryParse<CafeteriaLocation>(employeeCafeteriaLoc, true, out var location))
            {
                return (false, "Your account is not properly configured for cafeteria management.");
            }

            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(location);
            if (cafeteria == null)
                return (false, "Unable to find your cafeteria location.");

            // Validate products
            dto.ExampleProducts = dto.ExampleProducts
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
            if (dto.ExampleProducts.Count == 0)
            {
                return (false, "At least one product is required");
            }

            // Check if hot meals allowed
            if (dto.MealType == MealType.HotMeal && !cafeteria.OffersHotMeals)
            {
                return (false, "Your location does not offer hot meals.");
            }

            // Must reserve before pickup
            if (dto.LastReservationDateTime >= dto.PickupDateTime)
            {
                return (false, "Last reservation time must be before pickup time.");
            }

            // Build new package
            var newPackage = new Package
            {
                Name = dto.Name,
                City = cafeteria.City, // Set based on cafeteria's city
                CafeteriaLocation = location,
                PickupDateTime = dto.PickupDateTime,
                LastReservationDateTime = dto.LastReservationDateTime,
                IsAdultOnly = dto.IsAdultOnly,
                Price = dto.Price,
                MealType = dto.MealType,
                CafeteriaId = cafeteria.Id,
                Products = dto.ExampleProducts
                    .Select(name => new Product { Name = name })
                    .ToList()
            };

            await _packageRepository.AddAsync(newPackage);
            return (true, "Package created successfully.");
        }

        public async Task<(bool Found, bool Reserved, CreatePackageDto? Dto, string? ErrorMessage)>
            GetEditPackageAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null)
                return (false, false, null, null);

            if (package.Reservation != null)
                return (true, true, null, "Cannot edit a package that is already reserved.");

            // Retrieve cafeteria to get the city
            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(package.CafeteriaLocation);
            if (cafeteria == null)
                return (false, false, null, "Cafeteria not found.");

            var dto = new CreatePackageDto
            {
                Name = package.Name,
                PickupDateTime = package.PickupDateTime,
                LastReservationDateTime = package.LastReservationDateTime,
                IsAdultOnly = package.IsAdultOnly,
                Price = package.Price,
                MealType = package.MealType,
                ExampleProducts = package.Products?.Select(p => p.Name).ToList() ?? new List<string>()
            };
            return (true, false, dto, null);
        }

        public async Task<(bool Success, string ErrorMessage)> UpdatePackageAsync(
            int packageId,
            CreatePackageDto dto,
            string employeeId)
        {
            var package = await _packageRepository.GetByIdAsync(packageId);
            if (package == null)
                return (false, "Package not found.");

            // Already reserved?
            if (package.Reservation != null)
                return (false, "Cannot edit a package that is already reserved.");

            // Retrieve employee's cafeteria location
            var employeeCafeteriaLoc = await _currentUserService.GetCafeteriaLocationAsync(employeeId);
            if (string.IsNullOrEmpty(employeeCafeteriaLoc))
                return (false, "User not found or cafeteria location not set.");

            if (!Enum.TryParse<CafeteriaLocation>(employeeCafeteriaLoc, true, out var location))
            {
                return (false, "Your account is not properly configured for cafeteria management.");
            }

            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(location);
            if (cafeteria == null)
                return (false, "Unable to find cafeteria location.");

            // Validate products
            dto.ExampleProducts = dto.ExampleProducts
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
            if (dto.ExampleProducts.Count == 0)
            {
                return (false, "At least one product is required");
            }

            if (dto.MealType == MealType.HotMeal && !cafeteria.OffersHotMeals)
            {
                return (false, "Your location does not offer hot meals.");
            }

            if (dto.LastReservationDateTime >= dto.PickupDateTime)
            {
                return (false, "Last reservation time must be before pickup time.");
            }

            // Update
            package.Name = dto.Name;
            package.PickupDateTime = dto.PickupDateTime;
            package.LastReservationDateTime = dto.LastReservationDateTime;
            package.IsAdultOnly = dto.IsAdultOnly;
            package.Price = dto.Price;
            package.MealType = dto.MealType;

            package.Products.Clear();
            foreach (var productName in dto.ExampleProducts)
            {
                package.Products.Add(new Product { Name = productName });
            }

            await _packageRepository.UpdateAsync(package);
            return (true, "Package updated successfully.");
        }

        public async Task<(bool Success, string ErrorMessage)> DeletePackageAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null)
                return (false, "Package not found.");

            if (package.Reservation != null)
                return (false, "Cannot delete a package that is already reserved.");

            await _packageRepository.DeleteAsync(id);
            return (true, "Package deleted successfully.");
        }

        public async Task<(bool Success, string ErrorMessage)> MarkAsPickedUpAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package?.Reservation == null)
                return (false, "Package or reservation not found.");

            package.Reservation.IsPickedUp = true;
            await _packageRepository.UpdateAsync(package);

            return (true, "Package marked as picked up.");
        }

        public async Task<(bool Success, string ErrorMessage)> MarkAsNoShowAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package?.Reservation == null)
                return (false, "Package or reservation not found.");

            if (package.Reservation.IsNoShow)
                return (false, "Already marked as no-show.");

            package.Reservation.IsNoShow = true;
            var student = await _studentService.GetStudentByNumberAsync(package.Reservation.StudentNumber);
            if (student != null)
            {
                await _studentService.UpdateNoShowCountAsync(
                    student.StudentNumber,
                    student.NoShowCount + 1);
            }

            await _packageRepository.UpdateAsync(package);
            return (true, "Package marked as no-show.");
        }

        public async Task<(bool Success, string ErrorMessage)> UndoNoShowAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package?.Reservation == null)
                return (false, "Package or reservation not found.");

            if (!package.Reservation.IsNoShow)
                return (false, "This package is not marked as no-show.");

            package.Reservation.IsNoShow = false;
            // Optionally reset IsPickedUp based on your business logic
            package.Reservation.IsPickedUp = false;

            var student = await _studentService.GetStudentByNumberAsync(package.Reservation.StudentNumber);
            if (student != null && student.NoShowCount > 0)
            {
                await _studentService.UpdateNoShowCountAsync(
                    student.StudentNumber,
                    student.NoShowCount - 1);
            }

            await _packageRepository.UpdateAsync(package);
            return (true, "No-show status undone successfully.");
        }
    }
}
