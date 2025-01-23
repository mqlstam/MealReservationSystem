using Domain.Enums;
using System;
using System.Collections.Generic;

namespace Application.DTOs.PackageManagement
{
    public class PackageListViewModel
    {
        public List<PackageManagementViewModel> Packages { get; set; } = new();
        public City? CityFilter { get; set; }
        public MealType? TypeFilter { get; set; }
        public decimal? MaxPriceFilter { get; set; }
        public bool ShowExpired { get; set; }
    }

    public class PackageManagementViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public City City { get; set; }
        public CafeteriaLocation CafeteriaLocation { get; set; }
        public DateTime PickupDateTime { get; set; }
        public DateTime LastReservationDateTime { get; set; }
        public bool IsAdultOnly { get; set; }
        public decimal Price { get; set; }
        public MealType MealType { get; set; }
        public List<string> Products { get; set; } = new();
        public bool IsReserved { get; set; }
        public bool IsPickedUp { get; set; }
        public bool IsNoShow { get; set; }
        public string? ReservedBy { get; set; }
        public bool IsExpired => PickupDateTime < DateTime.Now;
    }
}
