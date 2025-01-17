using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Reservation
{
    public int Id { get; set; }
    
    [Required]
    public string StudentNumber { get; set; } = string.Empty;
    
    public DateTime ReservationDateTime { get; set; }
    
    public bool IsPickedUp { get; set; }
    
    public bool IsNoShow { get; set; }
    
    public int PackageId { get; set; }
    public Package Package { get; set; } = null!;
    
    public Student Student { get; set; } = null!;
}
