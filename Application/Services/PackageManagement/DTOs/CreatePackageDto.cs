using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.Services.PackageManagement.DTOs
{
    /// <summary>
    /// DTO used in the Application layer for creating or editing a package.
    /// Excludes City and CafeteriaLocation as these are set based on the employee's assigned cafeteria.
    /// </summary>
    public class CreatePackageDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime PickupDateTime { get; set; }

        [Required]
        public DateTime LastReservationDateTime { get; set; }

        public bool IsAdultOnly { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public MealType MealType { get; set; }

        public List<string> ExampleProducts { get; set; } = new();
    }
}
