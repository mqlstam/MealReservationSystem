using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CafeteriaRepository : ICafeteriaRepository
{
    private readonly IApplicationDbContext _context;

    public CafeteriaRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Cafeteria>> GetAllAsync()
    {
        return await _context.Cafeterias
            .Include(c => c.Packages)
            .ToListAsync();
    }

    public async Task<Cafeteria?> GetByIdAsync(int id)
    {
        return await _context.Cafeterias
            .Include(c => c.Packages)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cafeteria?> GetByLocationAsync(CafeteriaLocation location)
    {
        return await _context.Cafeterias
            .Include(c => c.Packages)
            .FirstOrDefaultAsync(c => c.Location == location);
    }

    public async Task<bool> OffersHotMealsAsync(CafeteriaLocation location)
    {
        var cafeteria = await _context.Cafeterias
            .FirstOrDefaultAsync(c => c.Location == location);
        return cafeteria?.OffersHotMeals ?? false;
    }

    public async Task<Cafeteria> AddAsync(Cafeteria cafeteria)
    {
        _context.Cafeterias.Add(cafeteria);
        await _context.SaveChangesAsync();
        return cafeteria;
    }

    public async Task UpdateAsync(Cafeteria cafeteria)
    {
        _context.Cafeterias.Update(cafeteria);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var cafeteria = await _context.Cafeterias.FindAsync(id);
        if (cafeteria != null)
        {
            _context.Cafeterias.Remove(cafeteria);
            await _context.SaveChangesAsync();
        }
    }
}
