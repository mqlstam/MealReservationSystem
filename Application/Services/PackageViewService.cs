using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.DTOs.Common;
using Application.DTOs.Packages;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class PackageViewService : IPackageViewService
{
    private readonly IPackageRepository _packageRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IStudentService _studentService;

    public PackageViewService(
        IPackageRepository packageRepository,
        IReservationRepository reservationRepository,
        IStudentService studentService)
    {
        _packageRepository = packageRepository;
        _reservationRepository = reservationRepository;
        _studentService = studentService;
    }

    public async Task<IEnumerable<AvailablePackageDto>> GetAvailablePackagesAsync(
        string studentId, 
        PackageFilterDto filter)
    {
        var student = await _studentService.GetStudentByIdentityIdAsync(studentId);
        if (student == null)
            return Enumerable.Empty<AvailablePackageDto>();

        var packages = await _packageRepository.GetAvailablePackagesAsync();
        var filteredPackages = packages.AsQueryable();

        // Apply filters
        if (filter.CityFilter.HasValue)
            filteredPackages = filteredPackages.Where(p => p.City == filter.CityFilter);

        if (filter.TypeFilter.HasValue)
            filteredPackages = filteredPackages.Where(p => p.MealType == filter.TypeFilter);

        if (filter.MaxPriceFilter.HasValue)
            filteredPackages = filteredPackages.Where(p => p.Price <= filter.MaxPriceFilter);

        var dtos = new List<AvailablePackageDto>();
        
        foreach (var package in filteredPackages)
        {
            var hasReservationForDate = await _reservationRepository
                .HasReservationForDateAsync(studentId, package.PickupDateTime.Date);
            
            var noShowCount = await _reservationRepository.GetNoShowCountAsync(studentId);
            
            var dto = new AvailablePackageDto
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
                CanReserve = true
            };

            // Check reservation rules
            if (hasReservationForDate)
            {
                dto.CanReserve = false;
                dto.ReservationBlockReason = "You already have a reservation for this date";
            }
            else if (noShowCount >= 2)
            {
                dto.CanReserve = false;
                dto.ReservationBlockReason = "You have too many no-shows to make reservations";
            }
            else if (package.IsAdultOnly && !student.IsOfLegalAge)
            {
                dto.CanReserve = false;
                dto.ReservationBlockReason = "This package is restricted to users 18 and older";
            }

            dtos.Add(dto);
        }

        return dtos.OrderBy(p => p.PickupDateTime);
    }

    public async Task<IEnumerable<StudentReservationDto>> GetStudentReservationsAsync(string studentId)
    {
        var reservations = await _reservationRepository.GetByStudentIdAsync(studentId);
        
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

    public async Task<int> GetStudentNoShowCountAsync(string studentId)
    {
        return await _reservationRepository.GetNoShowCountAsync(studentId);
    }
}
