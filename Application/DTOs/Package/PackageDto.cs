using Domain.Enums;

namespace Application.DTOs.Package;

public class PackageDto
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
    public bool IsPickedUp { get; set; }
    public string? ReservedBy { get; set; }
}
