using Domain.Enums;

namespace WebApp.Models.Package;

public abstract class PackageBaseViewModel
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
    public List<string> ExampleProducts { get; set; } = new();
    public List<string> Products { get; set; } = new();
}
