using Domain.Enums;

namespace Application.DTOs.Common;

public class PackageFilterDto
{
    public City? CityFilter { get; set; }
    public MealType? TypeFilter { get; set; }
    public decimal? MaxPriceFilter { get; set; }
}
