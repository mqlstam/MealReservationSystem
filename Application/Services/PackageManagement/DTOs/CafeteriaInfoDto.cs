using System;

namespace Application.Services.PackageManagement.DTOs
{
    public class CafeteriaInfoDto
    {
        public string CityName { get; set; } = string.Empty;
        public string CafeteriaLocationName { get; set; } = string.Empty;
        public bool OffersHotMeals { get; set; }
    }
}

