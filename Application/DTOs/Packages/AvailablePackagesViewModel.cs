using Domain.Enums;

namespace Application.DTOs.Packages
{
    public class AvailablePackagesViewModel
    {
        public List<AvailablePackageDto> Packages { get; set; } = new();
        
        public City? CityFilter { get; set; }
        
        public MealType? TypeFilter { get; set; }
        
        public decimal? MaxPriceFilter { get; set; }
    }
}
