using Domain.Entities;

namespace Application.Common.Interfaces.Services;

public interface INoShowService
{
    Task ProcessNoShowAsync(Reservation reservation);
    Task<bool> CanStudentReserveAsync(string studentId);
    Task<int> GetNoShowCountAsync(string studentId);
    Task UndoNoShowAsync(Reservation reservation);
}
