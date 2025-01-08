using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IStudentService
{
    Task<Student> GetOrCreateStudentAsync(string identityId, string studentNumber, string email, 
        string firstName, string lastName, DateTime dateOfBirth, Domain.Enums.City studyCity);
    Task<Student?> GetStudentByIdentityIdAsync(string identityId);
    Task<Student?> GetStudentByNumberAsync(string studentNumber);
    Task UpdateNoShowCountAsync(string studentNumber, int noShowCount);
}
