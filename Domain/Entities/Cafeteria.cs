using Domain.Enums;

namespace Domain.Entities;

public class Cafeteria
{
    public int Id { get; set; }
    
    public City City { get; set; }
    
    public CafeteriaLocation Location { get; set; }
    
    public bool OffersHotMeals { get; set; }
    
    public ICollection<Package> Packages { get; set; } = new List<Package>();
}
