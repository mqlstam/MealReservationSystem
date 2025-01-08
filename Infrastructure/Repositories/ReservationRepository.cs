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
            .ThenInclude(p => p.Products)
            .Include(r => r.Student)
            .OrderByDescending(r => r.ReservationDateTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetByStudentIdAsync(string identityId)
    {
        // First find the student by their identity ID
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.IdentityId == identityId);

        if (student == null)
            return new List<Reservation>();

        return await _context.Reservations
            .Include(r => r.Package)
            .ThenInclude(p => p.Products)
            .Include(r => r.Student)
            .Where(r => r.StudentNumber == student.StudentNumber)
            .OrderByDescending(r => r.ReservationDateTime)
            .ToListAsync();
    }

    public async Task<Reservation?> GetByIdAsync(int id)
    {
        return await _context.Reservations
            .Include(r => r.Package)
            .ThenInclude(p => p.Products)
            .Include(r => r.Student)
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

    public async Task<bool> HasReservationForDateAsync(string identityId, DateTime date)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.IdentityId == identityId);

        if (student == null)
            return false;

        return await _context.Reservations
            .Include(r => r.Package)
            .AnyAsync(r => r.StudentNumber == student.StudentNumber && 
                          r.Package.PickupDateTime.Date == date.Date);
    }

    public async Task<int> GetNoShowCountAsync(string identityId)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.IdentityId == identityId);

        return student?.NoShowCount ?? 0;
    }
}
