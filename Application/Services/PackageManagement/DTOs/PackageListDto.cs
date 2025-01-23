using Domain.Enums;
using System.Collections.Generic;
using System;

namespace Application.Services.PackageManagement.DTOs
{
    /// <summary>
    /// DTO for returning a list of packages in the Application layer
    /// (instead of referencing PackageListViewModel from WebApp).
    /// </summary>
    public class PackageListDto
    {
        public List<PackageManagementDto> Packages { get; set; } = new();
        public City? CityFilter { get; set; }
        public MealType? TypeFilter { get; set; }
        public decimal? MaxPriceFilter { get; set; }
        public bool ShowExpired { get; set; }
    }

    /// <summary>
    /// Represents one package in the list that the employee sees.
    /// </summary>
    public class PackageManagementDto
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
        
        // Reservation info
        public bool IsReserved { get; set; }
        public bool IsPickedUp { get; set; }
        public bool IsNoShow { get; set; }
        public string? ReservedBy { get; set; }

        public bool IsExpired => !IsReserved && DateTime.Now > LastReservationDateTime;
        public string Status
        {
            get
            {
                if (IsNoShow) return "No-Show";
                if (IsPickedUp) return "Picked Up";
                if (IsExpired) return "Expired";
                if (IsReserved) return "Reserved";
                return "Available";
            }
        }
    }
}
