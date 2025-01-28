using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PackageRepository : IPackageRepository
{
    private readonly IApplicationDbContext _context;

    public PackageRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Package>> GetAllAsync()
    {
        return await _context.Packages
            .Include(p => p.Products)
            .Include(p => p.Cafeteria)
            .Include(p => p.Reservation)
            .OrderBy(p => p.PickupDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Package>> GetAvailablePackagesAsync()
    {
        return await _context.Packages
            .AsNoTracking()    
            .Include(p => p.Products)
            .Include(p => p.Cafeteria)
            .AsNoTracking()
            .Where(p => p.Reservation == null && p.PickupDateTime > DateTime.Now)
            .OrderBy(p => p.PickupDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Package>> GetByLocationAsync(CafeteriaLocation location)
    {
        return await _context.Packages
            .Include(p => p.Products)
            .Include(p => p.Cafeteria)
            .Include(p => p.Reservation)
            .Where(p => p.CafeteriaLocation == location)
            .OrderBy(p => p.PickupDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Package>> GetByMealTypeAsync(MealType mealType)
    {
        return await _context.Packages
            .Include(p => p.Products)
            .Include(p => p.Cafeteria)
            .Include(p => p.Reservation)
            .Where(p => p.MealType == mealType)
            .OrderBy(p => p.PickupDateTime)
            .ToListAsync();
    }

    public async Task<Package?> GetByIdAsync(int id)
    {
        return await _context.Packages
            .Include(p => p.Products)
            .Include(p => p.Cafeteria)
            .Include(p => p.Reservation)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Package> AddAsync(Package package)
    {
        _context.Packages.Add(package);
        await _context.SaveChangesAsync();
        return package;
    }

    public async Task UpdateAsync(Package package)
    {
        _context.Packages.Update(package);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var package = await _context.Packages.FindAsync(id);
        if (package != null)
        {
            _context.Packages.Remove(package);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Packages.AnyAsync(p => p.Id == id);
    }
}
