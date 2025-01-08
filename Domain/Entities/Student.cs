using Domain.Enums;

namespace Domain.Entities;

public class Student
{
    public string StudentNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public City StudyCity { get; set; }
    public int NoShowCount { get; set; }
    public string IdentityId { get; set; } = string.Empty;

    // Navigation property
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    // Helper method to check if student is eligible for alcohol
    public bool IsOfLegalAge => DateTime.Today.AddYears(-18) >= DateOfBirth;
}
