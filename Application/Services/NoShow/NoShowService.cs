using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.NoShow;

public class NoShowService : INoShowService
{
    private readonly IStudentService _studentService;
    private readonly IApplicationDbContext _context;
    private const int MaxNoShows = 2;

    public NoShowService(
        IStudentService studentService,
        IApplicationDbContext context)
    {
        _studentService = studentService;
        _context = context;
    }

    public async Task ProcessNoShowAsync(Reservation reservation)
    {
        if (reservation.IsNoShow)
            return;

        var student = await _studentService.GetStudentByNumberAsync(reservation.StudentNumber);
        if (student == null)
            throw new InvalidOperationException("Student not found");

        // Update reservation
        reservation.IsNoShow = true;
        
        // Increment student's no-show count
        await _studentService.UpdateNoShowCountAsync(
            student.StudentNumber,
            student.NoShowCount + 1
        );

        await _context.SaveChangesAsync();
    }

    public async Task<bool> CanStudentReserveAsync(string studentId)
    {
        var student = await _studentService.GetStudentByIdentityIdAsync(studentId);
        return student?.NoShowCount < MaxNoShows;
    }

    public async Task<int> GetNoShowCountAsync(string studentId)
    {
        var student = await _studentService.GetStudentByIdentityIdAsync(studentId);
        return student?.NoShowCount ?? 0;
    }

    public async Task UndoNoShowAsync(Reservation reservation)
    {
        if (!reservation.IsNoShow)
            return;

        var student = await _studentService.GetStudentByNumberAsync(reservation.StudentNumber);
        if (student == null)
            throw new InvalidOperationException("Student not found");

        // Update reservation
        reservation.IsNoShow = false;
        
        // Decrement student's no-show count
        if (student.NoShowCount > 0)
        {
            await _studentService.UpdateNoShowCountAsync(
                student.StudentNumber,
                student.NoShowCount - 1
            );
        }

        await _context.SaveChangesAsync();
    }
}
