namespace WebApi.DTOs;

public class ReservationDto
{
    public int Id { get; set; }
    public int PackageId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public DateTime ReservationDateTime { get; set; }
    public bool IsPickedUp { get; set; }
    public bool IsNoShow { get; set; }
}
