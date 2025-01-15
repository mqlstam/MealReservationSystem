using Domain.Enums;

namespace WebApp.Models.Package;

public class PackageViewModel : PackageBaseViewModel
{
    public bool IsReserved { get; set; }
    public bool IsPickedUp { get; set; }
    public List<string> ExampleProducts { get; set; } = new();
}
