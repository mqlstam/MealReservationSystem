using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Services.PackageManagement.DTOs;
using Domain.Entities;
using Domain.Enums;

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
            ICurrentUserService currentUserService
        )
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
            bool showExpired
        )
        {
            var employeeCafeteriaLoc = await _currentUserService.GetCafeteriaLocationAsync(employeeId);
            if (string.IsNullOrEmpty(employeeCafeteriaLoc))
            {
                return new PackageListDto { Packages = new List<PackageManagementDto>() };
            }

            if (!Enum.TryParse<CafeteriaLocation>(employeeCafeteriaLoc, out var location))
            {
                return new PackageListDto { Packages = new List<PackageManagementDto>() };
            }

            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(location);
            if (cafeteria == null)
            {
                return new PackageListDto { Packages = new List<PackageManagementDto>() };
            }

            var allPackages = await _packageRepository.GetAllAsync();
            var result = new List<PackageManagementDto>();

            foreach (var pkg in allPackages)
            {
                string? reservedBy = null;
                if (pkg.Reservation != null)
                {
                    var stud = await _studentService.GetStudentByNumberAsync(pkg.Reservation.StudentNumber);
                    if (stud != null)
                    {
                        reservedBy = $"{stud.FirstName} {stud.LastName}";
                    }
                }

                var dto = new PackageManagementDto
                {
                    Id = pkg.Id,
                    Name = pkg.Name,
                    City = pkg.City,
                    CafeteriaLocation = pkg.CafeteriaLocation,
                    PickupDateTime = pkg.PickupDateTime,
                    LastReservationDateTime = pkg.LastReservationDateTime,
                    IsAdultOnly = pkg.IsAdultOnly,
                    Price = pkg.Price,
                    MealType = pkg.MealType,
                    Products = pkg.Products.Select(p => p.Name).ToList(),
                    IsReserved = (pkg.Reservation != null),
                    IsPickedUp = pkg.Reservation?.IsPickedUp ?? false,
                    IsNoShow = pkg.Reservation?.IsNoShow ?? false,
                    ReservedBy = reservedBy
                };

                result.Add(dto);
            }

            if (showOnlyMyCafeteria)
            {
                result = result.Where(x => x.CafeteriaLocation == location).ToList();
            }

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

        public async Task<(bool Success, string ErrorMessage)> CreatePackageAsync(string employeeId, CreatePackageDto dto)
        {
            var employeeCafeteriaLoc = await _currentUserService.GetCafeteriaLocationAsync(employeeId);
            if (string.IsNullOrEmpty(employeeCafeteriaLoc))
            {
                return (false, "User not found or cafeteria location not set");
            }

            if (!Enum.TryParse<CafeteriaLocation>(employeeCafeteriaLoc, out var location))
            {
                return (false, "Your account is not properly configured for cafeteria management.");
            }

            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(location);
            if (cafeteria == null)
            {
                return (false, "Unable to find your cafeteria location.");
            }

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

            var newPackage = new Package
            {
                Name = dto.Name,
                City = cafeteria.City,
                CafeteriaLocation = location,
                PickupDateTime = dto.PickupDateTime,
                LastReservationDateTime = dto.LastReservationDateTime,
                Price = dto.Price,
                MealType = dto.MealType,
                CafeteriaId = cafeteria.Id,
                Products = dto.ExampleProducts
                    .Select(name => new Product {
                        Name = name,
                        IsAlcoholic = dto.AlcoholicProducts?.Contains(name) ?? false
                    })
                    .ToList()
            };

            newPackage.UpdateIsAdultOnly();
            await _packageRepository.AddAsync(newPackage);
            return (true, "Package created successfully.");
        }

        public async Task<(bool Found, bool Reserved, CreatePackageDto? Dto, string? ErrorMessage)>
            GetEditPackageAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null)
            {
                return (false, false, null, null);
            }
            if (package.Reservation != null)
            {
                return (true, true, null, "Cannot edit a package that is already reserved.");
            }

            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(package.CafeteriaLocation);
            if (cafeteria == null)
            {
                return (false, false, null, "Cafeteria not found.");
            }

            var dto = new CreatePackageDto
            {
                Name = package.Name,
                PickupDateTime = package.PickupDateTime,
                LastReservationDateTime = package.LastReservationDateTime,
                Price = package.Price,
                MealType = package.MealType,
                ExampleProducts = package.Products.Select(p => p.Name).ToList(),
                AlcoholicProducts = package.Products.Where(p => p.IsAlcoholic).Select(p => p.Name).ToList()
            };
            return (true, false, dto, null);
        }

        public async Task<(bool Success, string ErrorMessage)> UpdatePackageAsync(int packageId, CreatePackageDto dto, string employeeId)
        {
            var package = await _packageRepository.GetByIdAsync(packageId);
            if (package == null)
            {
                return (false, "Package not found.");
            }
            if (package.Reservation != null)
            {
                return (false, "Cannot edit a package that is already reserved.");
            }

            var employeeCafeteriaLoc = await _currentUserService.GetCafeteriaLocationAsync(employeeId);
            if (string.IsNullOrEmpty(employeeCafeteriaLoc))
            {
                return (false, "User not found or cafeteria location not set.");
            }

            if (!Enum.TryParse<CafeteriaLocation>(employeeCafeteriaLoc, out var location))
            {
                return (false, "Your account is not properly configured for cafeteria management.");
            }

            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(location);
            if (cafeteria == null)
            {
                return (false, "Unable to find cafeteria location.");
            }

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

            package.Name = dto.Name;
            package.PickupDateTime = dto.PickupDateTime;
            package.LastReservationDateTime = dto.LastReservationDateTime;
            package.Price = dto.Price;
            package.MealType = dto.MealType;

            package.Products.Clear();
            foreach (var productName in dto.ExampleProducts)
            {
                package.Products.Add(new Product {
                    Name = productName,
                    IsAlcoholic = dto.AlcoholicProducts?.Contains(productName) ?? false
                });
            }

            package.UpdateIsAdultOnly(); // Call the method after updating Products
            await _packageRepository.UpdateAsync(package);
            return (true, "Package updated successfully.");
        }

        public async Task<(bool Success, string ErrorMessage)> DeletePackageAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null)
            {
                return (false, "Package not found.");
            }
            if (package.Reservation != null)
            {
                return (false, "Cannot delete a package that is already reserved.");
            }

            await _packageRepository.DeleteAsync(id);
            return (true, "Package deleted successfully.");
        }

        public async Task<(bool Success, string ErrorMessage)> MarkAsPickedUpAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package?.Reservation == null)
            {
                return (false, "Package or reservation not found.");
            }

            package.Reservation.IsPickedUp = true;
            await _packageRepository.UpdateAsync(package);
            return (true, "Package marked as picked up.");
        }

        public async Task<(bool Success, string ErrorMessage)> MarkAsNoShowAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package?.Reservation == null)
            {
                return (false, "Package or reservation not found.");
            }
            if (package.Reservation.IsNoShow)
            {
                return (false, "Already marked as no-show.");
            }

            package.Reservation.IsNoShow = true;
            var student = await _studentService.GetStudentByNumberAsync(package.Reservation.StudentNumber);
            if (student != null)
            {
                await _studentService.UpdateNoShowCountAsync(student.StudentNumber, student.NoShowCount + 1);
            }

            await _packageRepository.UpdateAsync(package);
            return (true, "Package marked as no-show.");
        }

        public async Task<(bool Success, string ErrorMessage)> UndoNoShowAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package?.Reservation == null)
            {
                return (false, "Package or reservation not found.");
            }
            if (!package.Reservation.IsNoShow)
            {
                return (false, "This package is not marked as no-show.");
            }

            package.Reservation.IsNoShow = false;
            package.Reservation.IsPickedUp = false;

            var student = await _studentService.GetStudentByNumberAsync(package.Reservation.StudentNumber);
            if (student != null && student.NoShowCount > 0)
            {
                await _studentService.UpdateNoShowCountAsync(student.StudentNumber, student.NoShowCount - 1);
            }

            await _packageRepository.UpdateAsync(package);
            return (true, "No-show status undone successfully.");
        }

        public async Task<(bool Found, CafeteriaInfoDto? Cafeteria, string? ErrorMessage)> GetEmployeeCafeteriaAsync(string employeeId)
        {
            var locationString = await _currentUserService.GetCafeteriaLocationAsync(employeeId);
            if (string.IsNullOrEmpty(locationString))
            {
                return (false, null, "No cafeteria location found for this user.");
            }

            if (!Enum.TryParse(locationString, out CafeteriaLocation loc))
            {
                return (false, null, "Invalid cafeteria location for user.");
            }

            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(loc);
            if (cafeteria == null)
            {
                return (false, null, $"No cafeteria found for location {loc}.");
            }

            var dto = new CafeteriaInfoDto
            {
                CityName = cafeteria.City.ToString(),
                CafeteriaLocationName = cafeteria.Location.ToString(),
                OffersHotMeals = cafeteria.OffersHotMeals
            };
            return (true, dto, null);
        }
    }
}
