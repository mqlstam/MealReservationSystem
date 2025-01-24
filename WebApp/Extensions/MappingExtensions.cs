using Application.DTOs.Package;
using Application.DTOs.PackageManagement;
using Application.DTOs.Packages;
using Application.DTOs.Reservation;

namespace WebApp.Extensions;

public static class MappingExtensions
{
    public static PackageViewModel ToViewModel(this PackageDto dto, bool canReserve = true, string? reservationBlockReason = null)
    {
        return new PackageViewModel
        {
            Package = dto,
            CanReserve = canReserve,
            ReservationBlockReason = reservationBlockReason
        };
    }

    public static ReservationListViewModel ToViewModel(this IEnumerable<ReservationDto> dtos, int noShowCount)
    {
        return new ReservationListViewModel
        {
            Reservations = dtos.ToList(),
            NoShowCount = noShowCount
        };
    }

    public static PackageListViewModel ToViewModel(
        this IEnumerable<PackageDto> dtos, 
        Domain.Enums.City? cityFilter = null,
        Domain.Enums.MealType? typeFilter = null,
        decimal? maxPriceFilter = null,
        bool showExpired = false)
    {
        return new PackageListViewModel
        {
            Packages = dtos.Select(dto => new PackageManagementViewModel
            {
                Id = dto.Id,
                Name = dto.Name,
                City = dto.City,
                CafeteriaLocation = dto.CafeteriaLocation,
                PickupDateTime = dto.PickupDateTime,
                LastReservationDateTime = dto.LastReservationDateTime,
                IsAdultOnly = dto.IsAdultOnly,
                Price = dto.Price,
                MealType = dto.MealType,
                Products = dto.Products,
                IsReserved = dto.IsReserved,
                IsPickedUp = dto.IsPickedUp,
                IsNoShow = false, // for example, set as needed
                ReservedBy = dto.ReservedBy
            }).ToList(),
            CityFilter = cityFilter,
            TypeFilter = typeFilter,
            MaxPriceFilter = maxPriceFilter,
            ShowExpired = showExpired
        };
    }
}
