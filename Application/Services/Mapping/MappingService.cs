using Application.Common.Interfaces;
using Application.DTOs.Account;
using Application.DTOs.Package;
using Application.DTOs.Packages;
using Application.DTOs.Reservation;
using Domain.Entities;

namespace Application.Services.Mapping
{
    public class MappingService : IMappingService
    {
        public PackageDto MapToDto(Package package)
        {
            return new PackageDto
            {
                Id = package.Id,
                Name = package.Name,
                City = package.City,
                CafeteriaLocation = package.CafeteriaLocation,
                PickupDateTime = package.PickupDateTime,
                LastReservationDateTime = package.LastReservationDateTime,
                IsAdultOnly = package.IsAdultOnly,  // Make sure this is being mapped
                Price = package.Price,
                MealType = package.MealType,
                Products = package.Products.Select(p => p.Name).ToList(),
                IsReserved = (package.Reservation != null),
                IsPickedUp = package.Reservation?.IsPickedUp ?? false,
                ReservedBy = package.Reservation?.Student != null
                    ? $"{package.Reservation.Student.FirstName} {package.Reservation.Student.LastName}"
                    : null
            };
        }

        public ReservationDto MapToDto(Reservation reservation)
        {
            return new ReservationDto
            {
                Id = reservation.Id,
                PackageName = reservation.Package.Name,
                City = reservation.Package.City,
                Location = reservation.Package.CafeteriaLocation,
                PickupDateTime = reservation.Package.PickupDateTime,
                Price = reservation.Package.Price,
                MealType = reservation.Package.MealType,
                Products = reservation.Package.Products.Select(p => p.Name).ToList(),
                IsPickedUp = reservation.IsPickedUp,
                IsNoShow = reservation.IsNoShow
            };
        }

        public AvailablePackageDto MapToAvailablePackageDto(Package package)
        {
            return new AvailablePackageDto
            {
                Id = package.Id,
                Name = package.Name,
                City = package.City,
                CafeteriaLocation = package.CafeteriaLocation,
                PickupDateTime = package.PickupDateTime,
                LastReservationDateTime = package.LastReservationDateTime,
                IsAdultOnly = package.IsAdultOnly,
                Price = package.Price,
                MealType = package.MealType,
                ExampleProducts = package.Products.Select(p => p.Name).ToList(),
                CanReserve = true,
                ReservationBlockReason = null
            };
        }

        public Package MapToEntity(PackageDto dto)
        {
            return new Package
            {
                Id = dto.Id,
                Name = dto.Name,
                City = dto.City,
                CafeteriaLocation = dto.CafeteriaLocation,
                PickupDateTime = dto.PickupDateTime,
                LastReservationDateTime = dto.LastReservationDateTime,
                Price = dto.Price,
                MealType = dto.MealType
            };
        }

        public Reservation MapToEntity(ReservationDto dto)
        {
            return new Reservation
            {
                Id = dto.Id,
                IsPickedUp = dto.IsPickedUp,
                IsNoShow = dto.IsNoShow
            };
        }
    }
}
