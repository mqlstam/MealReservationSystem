using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Product
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public bool IsAlcoholic { get; set; }
    
    public string? PhotoUrl { get; set; }
    
    public ICollection<Package> Packages { get; set; } = new List<Package>();
}
