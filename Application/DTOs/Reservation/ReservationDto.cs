using Domain.Enums;

namespace Application.DTOs.Reservation;

public class ReservationDto
{
    public int Id { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public City City { get; set; }
    public CafeteriaLocation Location { get; set; }
    public DateTime PickupDateTime { get; set; }
    public decimal Price { get; set; }
    public MealType MealType { get; set; }
    public List<string> Products { get; set; } = new();
    public bool IsPickedUp { get; set; }
    public bool IsNoShow { get; set; }
}
