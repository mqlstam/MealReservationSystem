using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PackageRepository : IPackageRepository
{
    private readonly IApplicationDbContext _context;
    private readonly IStudentService _studentService;

    public PackageRepository(
        IApplicationDbContext context,
        IStudentService studentService)
    {
        _context = context;
        _studentService = studentService;
    }

    public async Task<IEnumerable<Package>> GetAllAsync()
    {
        var packages = await _context.Packages
            .Include(p => p.Products)
            .Include(p => p.Cafeteria)
            .Include(p => p.Reservation)
            .OrderBy(p => p.PickupDateTime)
            .ToListAsync();

        // Process any potential no-shows
        foreach (var package in packages)
        {
            await CheckAndProcessNoShow(package);
        }

        return packages;
    }

    public async Task<IEnumerable<Package>> GetAvailablePackagesAsync()
    {
        var packages = await _context.Packages
            .Include(p => p.Products)
            .Include(p => p.Cafeteria)
            .Where(p => p.Reservation == null && p.PickupDateTime > DateTime.Now)
            .OrderBy(p => p.PickupDateTime)
            .ToListAsync();

        // Optional: If you also want to check for missed no-shows here, do so
        foreach (var package in packages)
        {
            await CheckAndProcessNoShow(package);
        }

        return packages;
    }

    public async Task<IEnumerable<Package>> GetByLocationAsync(CafeteriaLocation location)
    {
        var packages = await _context.Packages
            .Include(p => p.Products)
            .Include(p => p.Cafeteria)
            .Include(p => p.Reservation)
            .Where(p => p.CafeteriaLocation == location)
            .OrderBy(p => p.PickupDateTime)
            .ToListAsync();

        // Process no-shows for these packages
        foreach (var package in packages)
        {
            await CheckAndProcessNoShow(package);
        }

        return packages;
    }

    public async Task<IEnumerable<Package>> GetByMealTypeAsync(MealType mealType)
    {
        var packages = await _context.Packages
            .Include(p => p.Products)
            .Include(p => p.Cafeteria)
            .Include(p => p.Reservation)
            .Where(p => p.MealType == mealType)
            .OrderBy(p => p.PickupDateTime)
            .ToListAsync();

        foreach (var package in packages)
        {
            await CheckAndProcessNoShow(package);
        }

        return packages;
    }

    public async Task<Package?> GetByIdAsync(int id)
    {
        var package = await _context.Packages
            .Include(p => p.Products)
            .Include(p => p.Cafeteria)
            .Include(p => p.Reservation)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (package != null)
        {
            // Ensure we process no-show if the pickup time is passed
            await CheckAndProcessNoShow(package);
        }

        return package;
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

    /// <summary>
    /// Checks if the package's pickup time has passed and the reservation hasn't been picked up
    /// or marked as no-show. If so, mark it as no-show and increment the student's no-show count.
    /// </summary>
    private async Task CheckAndProcessNoShow(Package package)
    {
        if (package.Reservation != null
            && !package.Reservation.IsPickedUp
            && !package.Reservation.IsNoShow
            && package.PickupDateTime < DateTime.Now)
        {
            // Mark it as no-show
            package.Reservation.IsNoShow = true;

            // Increase student's no-show count
            var student = await _studentService.GetStudentByNumberAsync(package.Reservation.StudentNumber);
            if (student != null)
            {
                await _studentService.UpdateNoShowCountAsync(
                    student.StudentNumber, 
                    student.NoShowCount + 1
                );
            }

            // Persist changes
            await _context.SaveChangesAsync();
        }
    }
}
