using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? StudentNumber { get; set; }
    public string? EmployeeNumber { get; set; }
    public string? StudyCity { get; set; }
    public string? CafeteriaLocation { get; set; }
    public int NoShowCount { get; set; }
    public bool IsStudent => !string.IsNullOrEmpty(StudentNumber);
    public bool IsCafeteriaEmployee => !string.IsNullOrEmpty(EmployeeNumber);
}
