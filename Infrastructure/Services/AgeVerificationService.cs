using Application.Common.Interfaces.Services;
using Domain.Entities;

namespace Infrastructure.Services;

public class AgeVerificationService : IAgeVerificationService
{
    public bool IsStudentEligibleForPackage(Student student, Package package)
    {
        if (!package.IsAdultOnly)
            return true;

        return student.IsOfLegalAgeOn(package.PickupDateTime);
    }
}
