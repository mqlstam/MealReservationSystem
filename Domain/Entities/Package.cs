using Domain.Enums;

namespace Domain.Entities;

public class Package
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public City City { get; set; }
    
    public CafeteriaLocation CafeteriaLocation { get; set; }
    
    public DateTime PickupDateTime { get; set; }
    
    public DateTime LastReservationDateTime { get; set; }
    
    public bool IsAdultOnly { get; set; }
    
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
    
    public MealType MealType { get; set; }
    
    public ICollection<Product> Products { get; set; } = new List<Product>();
    
    public Reservation? Reservation { get; set; }
    
    public int CafeteriaId { get; set; }
    public Cafeteria Cafeteria { get; set; } = null!;
}
