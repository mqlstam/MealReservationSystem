using Domain.Enums;

namespace WebApp.Models.Package;

public class PackageBaseViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public City City { get; set; }
    public CafeteriaLocation CafeteriaLocation { get; set; }
    public DateTime PickupDateTime { get; set; }
    public DateTime LastReservationDateTime { get; set; }
    public bool IsAdultOnly { get; set; }
    public decimal Price { get; set; }
    public MealType MealType { get; set; }
    public List<string> Products { get; set; } = new();
    
    public bool HasExpired => DateTime.Now > LastReservationDateTime;
    public bool PickupExpired => DateTime.Now > PickupDateTime;
    
    public string StatusBadgeClass => GetStatusBadgeClass();
    public string StatusText => GetStatusText();
    
    private string GetStatusBadgeClass()
    {
        if (HasExpired) return "bg-danger";
        if (PickupExpired) return "bg-warning";
        return "bg-success";
    }
    
    private string GetStatusText()
    {
        if (HasExpired) return "Reservation Expired";
        if (PickupExpired) return "Pickup Time Passed";
        return "Available";
    }
}
