using Application.DTOs.Account;
using Application.DTOs.Package;
using Application.DTOs.Packages;
using Application.DTOs.Reservation;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IMappingService
{
    PackageDto MapToDto(Package package);
    ReservationDto MapToDto(Reservation reservation);
    AvailablePackageDto MapToAvailablePackageDto(Package package);
    Package MapToEntity(PackageDto dto);
    Reservation MapToEntity(ReservationDto dto);
}
