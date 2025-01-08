using Domain.Enums;

namespace WebApp.Models.Reservation;

public class AvailablePackagesViewModel
{
    public List<AvailablePackageItem> Packages { get; set; } = new();
    public City? CityFilter { get; set; }
    public MealType? TypeFilter { get; set; }
    public decimal? MaxPriceFilter { get; set; }
}

public class AvailablePackageItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public City City { get; set; }
    public CafeteriaLocation Location { get; set; }
    public DateTime PickupDateTime { get; set; }
    public DateTime LastReservationDateTime { get; set; }
    public bool IsAdultOnly { get; set; }
    public decimal Price { get; set; }
    public MealType MealType { get; set; }
    public List<string> ExampleProducts { get; set; } = new();
    public bool CanReserve { get; set; }
    public string? ReservationBlockReason { get; set; }
}
