using Application.DTOs.Common;
using Application.DTOs.Packages;

namespace Application.Common.Interfaces.Services;

public interface IPackageViewService
{
    Task<IEnumerable<AvailablePackageDto>> GetAvailablePackagesAsync(string studentId, PackageFilterDto filter);
    Task<IEnumerable<StudentReservationDto>> GetStudentReservationsAsync(string studentId);
    Task<int> GetStudentNoShowCountAsync(string studentId);
}
