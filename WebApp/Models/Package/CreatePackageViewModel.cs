using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace WebApp.Models.Package;

public class CreatePackageViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required")]
    public City City { get; set; }

    [Required(ErrorMessage = "Cafeteria location is required")]
    public CafeteriaLocation CafeteriaLocation { get; set; }

    [Required(ErrorMessage = "Pickup time is required")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Pickup Time")]
    [FutureDate(ErrorMessage = "Pickup time must be in the future")]
    [MaxAdvanceDays(2, ErrorMessage = "Packages can only be created maximum 2 days in advance")]
    public DateTime PickupDateTime { get; set; } = DateTime.Now.AddHours(1);

    [Required(ErrorMessage = "Last reservation time is required")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Last Reservation Time")]
    [FutureDate(ErrorMessage = "Last reservation time must be in the future")]
    public DateTime LastReservationDateTime { get; set; } = DateTime.Now.AddMinutes(30);

    [Display(Name = "18+ Only")]
    public bool IsAdultOnly { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, 100.00, ErrorMessage = "Price must be between €0.01 and €100.00")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Type of meal is required")]
    [Display(Name = "Type of Meal")]
    public MealType MealType { get; set; }

    [Required(ErrorMessage = "At least one product is required")]
    [MinLength(1, ErrorMessage = "At least one product is required")]
    [Display(Name = "Example Products")]
    public List<string> ExampleProducts { get; set; } = new();

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

    public class MaxAdvanceDaysAttribute : ValidationAttribute
    {
        private readonly int _maxDays;

        public MaxAdvanceDaysAttribute(int maxDays)
        {
            _maxDays = maxDays;
        }

        public override bool IsValid(object? value)
        {
            if (value is DateTime dateTime)
            {
                var maxDate = DateTime.Now.AddDays(_maxDays);
                return dateTime <= maxDate;
            }
            return false;
        }
    }
}
