using Domain.Enums;
using System;
using System.Collections.Generic;

namespace WebApp.Models.Package
{
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
