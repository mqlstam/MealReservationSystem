using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace WebApp.Models.Package;

public class CreatePackageViewModel
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public City City { get; set; }

    [Required]
    public CafeteriaLocation CafeteriaLocation { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Pickup Time")]
    [FutureDate(ErrorMessage = "Pickup time must be in the future")]
    public DateTime PickupDateTime { get; set; } = DateTime.Now.AddHours(1);

    [Required]
    [DataType(DataType.DateTime)]
    [Display(Name = "Last Reservation Time")]
    [FutureDate(ErrorMessage = "Last reservation time must be in the future")]
    public DateTime LastReservationDateTime { get; set; } = DateTime.Now.AddMinutes(30);

    [Display(Name = "18+ Only")]
    public bool IsAdultOnly { get; set; }

    [Required]
    [Range(0.01, 100.00)]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Required]
    [Display(Name = "Type of Meal")]
    public MealType MealType { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one product is required")]
    [Display(Name = "Example Products")]
    public List<string> ExampleProducts { get; set; } = new();
}

public class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is DateTime dateTime)
        {
            return dateTime > DateTime.Now;
        }
        return false;
    }
}
