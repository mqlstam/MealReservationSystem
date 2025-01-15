using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.DTOs.Common;
using Application.DTOs.Packages;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public class PackageViewService : IPackageViewService
{
    private readonly IPackageRepository _packageRepository;
    private readonly IStudentService _studentService;
    private readonly IReservationRepository _reservationRepository;

    public PackageViewService(
        IPackageRepository packageRepository,
        IStudentService studentService,
        IReservationRepository reservationRepository)
    {
        _packageRepository = packageRepository;
        _studentService = studentService;
        _reservationRepository = reservationRepository;
    }

    public async Task<IEnumerable<AvailablePackageDto>> GetAvailablePackagesAsync(string userId, PackageFilterDto filter)
    {
        var packages = await _packageRepository.GetAvailablePackagesAsync();
        var student = await _studentService.GetStudentByIdentityIdAsync(userId);
        
        if (student == null)
            return Enumerable.Empty<AvailablePackageDto>();

        // Filter packages
        var filteredPackages = packages.AsEnumerable();

        if (filter.CityFilter.HasValue)
            filteredPackages = filteredPackages.Where(p => p.City == filter.CityFilter.Value);
        
        if (filter.TypeFilter.HasValue)
            filteredPackages = filteredPackages.Where(p => p.MealType == filter.TypeFilter.Value);
        
        if (filter.MaxPriceFilter.HasValue)
            filteredPackages = filteredPackages.Where(p => p.Price <= filter.MaxPriceFilter.Value);

        var packagesList = filteredPackages.ToList();
        var result = new List<AvailablePackageDto>();

        foreach (var package in packagesList)
        {
            var hasReservation = await _reservationRepository.HasReservationForDateAsync(
                userId, package.PickupDateTime.Date);
            
            var noShowCount = await _reservationRepository.GetNoShowCountAsync(userId);
            
            var canReserve = true;
            string? reservationBlockReason = null;

            if (hasReservation)
            {
                canReserve = false;
                reservationBlockReason = "You already have a reservation for this date.";
            }
            else if (noShowCount >= 2)
            {
                canReserve = false;
                reservationBlockReason = "You cannot make reservations due to multiple no-shows.";
            }
            else if (package.IsAdultOnly && !student.IsOfLegalAge)
            {
                canReserve = false;
                reservationBlockReason = "This package is restricted to users 18 and older.";
            }

            result.Add(new AvailablePackageDto
            {
                Id = package.Id,
                Name = package.Name,
                City = package.City,
                Location = package.CafeteriaLocation,
                PickupDateTime = package.PickupDateTime,
                LastReservationDateTime = package.LastReservationDateTime,
                IsAdultOnly = package.IsAdultOnly,
                Price = package.Price,
                MealType = package.MealType,
                ExampleProducts = package.Products.Select(p => p.Name).ToList(),
                CanReserve = canReserve,
                ReservationBlockReason = reservationBlockReason
            });
        }

        return result.OrderBy(p => p.PickupDateTime);
    }

    public async Task<IEnumerable<StudentReservationDto>> GetStudentReservationsAsync(string userId)
    {
        var reservations = await _reservationRepository.GetByStudentIdAsync(userId);
        
        return reservations.Select(r => new StudentReservationDto
        {
            Id = r.Id,
            PackageName = r.Package.Name,
            City = r.Package.City,
            Location = r.Package.CafeteriaLocation,
            PickupDateTime = r.Package.PickupDateTime,
            Price = r.Package.Price,
            MealType = r.Package.MealType,
            Products = r.Package.Products.Select(p => p.Name).ToList(),
            IsPickedUp = r.IsPickedUp,
            IsNoShow = r.IsNoShow
        });
    }

    public async Task<int> GetStudentNoShowCountAsync(string userId)
    {
        return await _reservationRepository.GetNoShowCountAsync(userId);
    }
}
