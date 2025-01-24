using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Account;

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateTime DateOfBirth { get; set; }

    public string? PhoneNumber { get; set; }

    [Required]
    public bool IsStudent { get; set; } = true;

    public string? StudentNumber { get; set; }

    public string? StudyCity { get; set; }

    public string? EmployeeNumber { get; set; }

    public string? CafeteriaLocation { get; set; }
}
