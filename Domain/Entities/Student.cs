using Domain.Enums;

namespace Domain.Entities;

public class Student
{
    public string StudentNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }  // Added phone number
    public City StudyCity { get; set; }
    public int NoShowCount { get; set; }
    public string IdentityId { get; set; } = string.Empty;

    // Navigation property
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    // Helper method to check if student is eligible for adult packages on a specific date
    public bool IsOfLegalAgeOn(DateTime date)
    {
        var age = date.Year - DateOfBirth.Year;
        
        // Adjust age if birthday hasn't occurred this year
        if (DateOfBirth.Date > date.AddYears(-age))
            age--;
            
        return age >= 18;
    }

    // For backward compatibility and general age checks
    public bool IsOfLegalAge => IsOfLegalAgeOn(DateTime.Today);
}
