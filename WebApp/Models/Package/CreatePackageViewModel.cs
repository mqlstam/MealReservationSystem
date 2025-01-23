using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models.Package
{
    public class CreatePackageViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime PickupDateTime { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime LastReservationDateTime { get; set; }

        public bool IsAdultOnly { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public MealType MealType { get; set; }

        public List<string> ExampleProducts { get; set; } = new();
    }
}
