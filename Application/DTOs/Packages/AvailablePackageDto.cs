using Domain.Enums;

namespace Application.DTOs.Packages;

public class AvailablePackageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public City City { get; set; }
    
    // Rename 'Location' to 'CafeteriaLocation' so the view can do '@package.CafeteriaLocation'
    public CafeteriaLocation CafeteriaLocation { get; set; }
    
    public DateTime PickupDateTime { get; set; }
    public DateTime LastReservationDateTime { get; set; }
    public bool IsAdultOnly { get; set; }
    public decimal Price { get; set; }
    public MealType MealType { get; set; }
    
    public List<string> ExampleProducts { get; set; } = new();
    public bool CanReserve { get; set; }
    public string? ReservationBlockReason { get; set; }

    /// <summary>
    /// Add HasExpired so the view can check if the package is out of date.
    /// Adjust the condition as needed:
    /// e.g. if we consider it expired once pickup time is past or last reservation time is past.
    /// </summary>
    public bool HasExpired => PickupDateTime < DateTime.Now 
                              || LastReservationDateTime < DateTime.Now;
}
