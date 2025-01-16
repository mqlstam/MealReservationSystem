using Domain.Entities;

namespace Application.Common.Interfaces.Services;

public interface IAgeVerificationService
{
    bool IsStudentEligibleForPackage(Student student, Package package);
}
