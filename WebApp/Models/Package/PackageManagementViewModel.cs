using Domain.Enums;

namespace WebApp.Models.Package;

public class PackageManagementViewModel
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
    public bool IsReserved { get; set; }
    public bool HasExpired => DateTime.Now > LastReservationDateTime;
    public string? ReservedBy { get; set; }
    public bool IsPickedUp { get; set; }
    public string Status => GetStatus();

    private string GetStatus()
    {
        if (IsPickedUp) return "Picked Up";
        if (IsReserved) return "Reserved";
        if (HasExpired) return "Expired";
        return "Available";
    }
}
