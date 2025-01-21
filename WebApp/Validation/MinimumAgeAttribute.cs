using System.ComponentModel.DataAnnotations;

namespace WebApp.Validation;

public class MinimumAgeAttribute : ValidationAttribute
{
    private readonly int _minimumAge;

    public MinimumAgeAttribute(int minimumAge)
    {
        _minimumAge = minimumAge;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTime dateOfBirth)
        {
            var age = DateTime.Today.Year - dateOfBirth.Year;
            
            // Adjust age if birthday hasn't occurred this year
            if (dateOfBirth.Date > DateTime.Today.AddYears(-age))
            {
                age--;
            }

            if (age >= _minimumAge)
            {
                return ValidationResult.Success;
            }
        }

        return new ValidationResult($"You must be at least {_minimumAge} years old to register.");
    }
}
