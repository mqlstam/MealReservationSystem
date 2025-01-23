namespace WebApp.Models.Package;

public class PackageManagementViewModel : PackageBaseViewModel
{
    // Indicates if a student has reserved this package
    public bool IsReserved { get; set; }
    
    // Mark if the package is picked up
    public bool IsPickedUp { get; set; }
    
    // Mark if the package is flagged as "No-Show" (auto or manual)
    public bool IsNoShow { get; set; }

    // A package is Expired only if never reserved (IsReserved == false) 
    // and DateTime.Now > LastReservationDateTime
    public bool IsExpired => !IsReserved && DateTime.Now > LastReservationDateTime;

    // The user who reserved (if any)
    public string? ReservedBy { get; set; }

    // Show status text for employee’s dashboard
    public string Status => GetStatus();

    private string GetStatus()
    {
        if (IsNoShow) return "No-Show";
        if (IsPickedUp) return "Picked Up";
        if (IsExpired) return "Expired";
        if (IsReserved) return "Reserved";
        return "Available";
    }
}

