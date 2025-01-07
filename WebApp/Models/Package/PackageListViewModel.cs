using Domain.Enums;

namespace WebApp.Models.Package;

public class PackageListViewModel
{
    public List<PackageManagementViewModel> Packages { get; set; } = new();
    public City? CityFilter { get; set; }
    public MealType? TypeFilter { get; set; }
    public decimal? MaxPriceFilter { get; set; }
    public bool ShowExpired { get; set; }
}
