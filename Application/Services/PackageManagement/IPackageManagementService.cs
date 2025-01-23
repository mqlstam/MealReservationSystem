using System.Threading.Tasks;
using Domain.Enums;
using Application.Services.PackageManagement.DTOs;

namespace Application.Services.PackageManagement
{
    public interface IPackageManagementService
    {
        Task<PackageListDto> GetPackageListAsync(
            string employeeId,
            bool showOnlyMyCafeteria,
            City? cityFilter,
            MealType? typeFilter,
            decimal? maxPrice,
            bool showExpired
        );

        Task<(bool Success, string ErrorMessage)> CreatePackageAsync(string employeeId, CreatePackageDto dto);

        Task<(bool Found, bool Reserved, CreatePackageDto? Dto, string? ErrorMessage)>
            GetEditPackageAsync(int id);

        Task<(bool Success, string ErrorMessage)> UpdatePackageAsync(
            int packageId,
            CreatePackageDto dto,
            string employeeId
        );

        Task<(bool Success, string ErrorMessage)> DeletePackageAsync(int id);

        Task<(bool Success, string ErrorMessage)> MarkAsPickedUpAsync(int id);

        Task<(bool Success, string ErrorMessage)> MarkAsNoShowAsync(int id);

        Task<(bool Success, string ErrorMessage)> UndoNoShowAsync(int id);

        Task<(bool Found, CafeteriaInfoDto? Cafeteria, string? ErrorMessage)>
            GetEmployeeCafeteriaAsync(string employeeId);
    }
}


