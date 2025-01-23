using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IReservationRepository
{
    Task<IEnumerable<Reservation>> GetAllAsync();
    Task<IEnumerable<Reservation>> GetByStudentIdAsync(string studentId);
    Task<Reservation?> GetByIdAsync(int id);
    Task<Reservation> AddAsync(Reservation reservation);
    Task UpdateAsync(Reservation reservation);
    Task DeleteAsync(int id);
    Task<bool> HasReservationForDateAsync(string studentId, DateTime date);
    Task<int> GetNoShowCountAsync(string studentId);
}
