using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.Account;

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [Required]
    [Display(Name = "Register as Student")]
    public bool IsStudent { get; set; } = true;

    // Student-specific properties
    [Display(Name = "Student Number")]
    public string? StudentNumber { get; set; }

    [Display(Name = "Study City")]
    public string? StudyCity { get; set; }

    // Employee-specific properties
    [Display(Name = "Employee Number")]
    public string? EmployeeNumber { get; set; }

    [Display(Name = "Cafeteria Location")]
    public string? CafeteriaLocation { get; set; }
}
