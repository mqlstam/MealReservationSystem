using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.PackageManagement
{
    public class CreatePackageViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public City City { get; set; }

        [Required]
        public CafeteriaLocation CafeteriaLocation { get; set; }

        [Required]
        public DateTime PickupDateTime { get; set; }

        [Required]
        public DateTime LastReservationDateTime { get; set; }

        public bool IsAdultOnly { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }

        [Required]
        public MealType MealType { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one product is required.")]
        public List<string> ExampleProducts { get; set; } = new();
    }
}
