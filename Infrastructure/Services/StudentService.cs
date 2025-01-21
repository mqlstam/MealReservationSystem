using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _context;

    public StudentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Student> GetOrCreateStudentAsync(
        string identityId, string studentNumber, string email, 
        string firstName, string lastName, DateTime dateOfBirth, 
        Domain.Enums.City studyCity, string? phoneNumber)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.IdentityId == identityId);

        if (student == null)
        {
            student = new Student
            {
                StudentNumber = studentNumber,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth,
                StudyCity = studyCity,
                PhoneNumber = phoneNumber,
                IdentityId = identityId,
                NoShowCount = 0
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();
        }

        return student;
    }

    public async Task<Student?> GetStudentByIdentityIdAsync(string identityId)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.IdentityId == identityId);
    }

    public async Task<Student?> GetStudentByNumberAsync(string studentNumber)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber);
    }

    public async Task UpdateNoShowCountAsync(string studentNumber, int noShowCount)
    {
        var student = await GetStudentByNumberAsync(studentNumber);
        if (student != null)
        {
            student.NoShowCount = noShowCount;
            await _context.SaveChangesAsync();
        }
    }
}
