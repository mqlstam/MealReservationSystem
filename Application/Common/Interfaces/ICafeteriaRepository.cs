using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Interfaces;

public interface ICafeteriaRepository
{
    Task<IEnumerable<Cafeteria>> GetAllAsync();
    Task<Cafeteria?> GetByIdAsync(int id);
    Task<Cafeteria?> GetByLocationAsync(CafeteriaLocation location);
    Task<bool> OffersHotMealsAsync(CafeteriaLocation location);
    Task<Cafeteria> AddAsync(Cafeteria cafeteria);
    Task UpdateAsync(Cafeteria cafeteria);
    Task DeleteAsync(int id);
}
