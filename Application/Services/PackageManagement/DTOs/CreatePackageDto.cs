using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.Services.PackageManagement.DTOs
{
    public class CreatePackageDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime PickupDateTime { get; set; }

        [Required]
        public DateTime LastReservationDateTime { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public MealType MealType { get; set; }

        public List<string> ExampleProducts { get; set; } = new();
        
        // List to track which products contain alcohol
        public List<string> AlcoholicProducts { get; set; } = new();
    }
}
