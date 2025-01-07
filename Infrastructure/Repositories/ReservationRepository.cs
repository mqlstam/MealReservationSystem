using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly IApplicationDbContext _context;

    public ReservationRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Reservation>> GetAllAsync()
    {
        return await _context.Reservations
            .Include(r => r.Package)
            .ThenInclude(p => p!.Products)
            .OrderByDescending(r => r.ReservationDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetByStudentIdAsync(string studentId)
    {
        return await _context.Reservations
            .Include(r => r.Package)
            .ThenInclude(p => p!.Products)
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.ReservationDateTime)
            .ToListAsync();
    }

    public async Task<Reservation?> GetByIdAsync(int id)
    {
        return await _context.Reservations
            .Include(r => r.Package)
            .ThenInclude(p => p!.Products)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Reservation> AddAsync(Reservation reservation)
    {
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();
        return reservation;
    }

    public async Task UpdateAsync(Reservation reservation)
    {
        _context.Reservations.Update(reservation);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation != null)
        {
            _context.Reservations.Remove(reservation);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasReservationForDateAsync(string studentId, DateTime date)
    {
        return await _context.Reservations
            .Include(r => r.Package)
            .AnyAsync(r => r.StudentId == studentId && 
                          r.Package.PickupDateTime.Date == date.Date);
    }

    public async Task<int> GetNoShowCountAsync(string studentId)
    {
        return await _context.Reservations
            .CountAsync(r => r.StudentId == studentId && r.IsNoShow);
    }
}
