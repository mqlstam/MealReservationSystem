namespace WebApp.Models.Package;

public class PackageManagementViewModel : PackageBaseViewModel
{
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
