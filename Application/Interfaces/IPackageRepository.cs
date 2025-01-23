using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface IPackageRepository
{
    Task<IEnumerable<Package>> GetAllAsync();
    Task<IEnumerable<Package>> GetAvailablePackagesAsync();
    Task<IEnumerable<Package>> GetByLocationAsync(CafeteriaLocation location);
    Task<IEnumerable<Package>> GetByMealTypeAsync(MealType mealType);
    Task<Package?> GetByIdAsync(int id);
    Task<Package> AddAsync(Package package);
    Task UpdateAsync(Package package);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
