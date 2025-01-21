using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.DTOs.Common;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests.UserStories.US06;

public class PackageProductDisplayTests
{
    private readonly Mock<IPackageRepository> _mockPackageRepo;
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly Mock<IReservationRepository> _mockReservationRepo;
    private readonly IPackageViewService _packageViewService;

    public PackageProductDisplayTests()
    {
        _mockPackageRepo = new Mock<IPackageRepository>();
        _mockStudentService = new Mock<IStudentService>();
        _mockReservationRepo = new Mock<IReservationRepository>();
        
        _packageViewService = new PackageViewService(
            _mockPackageRepo.Object,
            _mockStudentService.Object,
            _mockReservationRepo.Object);
    }

    [Fact]
    public async Task Package_WithProducts_DisplaysAllProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Name = "Sandwich", IsAlcoholic = false },
            new() { Name = "Apple", IsAlcoholic = false },
            new() { Name = "Water", IsAlcoholic = false }
        };

        var package = new Package
        {
            Id = 1,
            Name = "Lunch Package",
            Products = products,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(2),
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            MealType = MealType.BreadAssortment,
            Price = 5.00m
        };

        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Now.AddYears(-20),
            IdentityId = "test-id"
        };

        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(new List<Package> { package });
        
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync("test-id"))
            .ReturnsAsync(student);

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync("test-id", new PackageFilterDto());
        var displayedPackage = result.First();

        // Assert
        Assert.Equal(3, displayedPackage.ExampleProducts.Count);
        Assert.Contains("Sandwich", displayedPackage.ExampleProducts);
        Assert.Contains("Apple", displayedPackage.ExampleProducts);
        Assert.Contains("Water", displayedPackage.ExampleProducts);
    }

    [Fact]
    public async Task Package_WithNoProducts_DisplaysEmptyList()
    {
        // Arrange
        var package = new Package
        {
            Id = 1,
            Name = "Empty Package",
            Products = new List<Product>(),
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(2),
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            MealType = MealType.BreadAssortment,
            Price = 5.00m
        };

        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Now.AddYears(-20),
            IdentityId = "test-id"
        };

        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(new List<Package> { package });
        
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync("test-id"))
            .ReturnsAsync(student);

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync("test-id", new PackageFilterDto());
        var displayedPackage = result.First();

        // Assert
        Assert.Empty(displayedPackage.ExampleProducts);
    }

    [Fact]
    public async Task Package_WithLongProductNames_DisplaysFullNames()
    {
        // Arrange
        var longProductName = "Extra Large Deluxe Club Sandwich with Special Sauce and Premium Ingredients";
        var products = new List<Product>
        {
            new() { Name = longProductName, IsAlcoholic = false }
        };

        var package = new Package
        {
            Id = 1,
            Name = "Lunch Package",
            Products = products,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(2),
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            MealType = MealType.BreadAssortment,
            Price = 5.00m
        };

        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Now.AddYears(-20),
            IdentityId = "test-id"
        };

        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(new List<Package> { package });
        
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync("test-id"))
            .ReturnsAsync(student);

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync("test-id", new PackageFilterDto());
        var displayedPackage = result.First();

        // Assert
        Assert.Single(displayedPackage.ExampleProducts);
        Assert.Equal(longProductName, displayedPackage.ExampleProducts[0]);
    }

    [Fact]
    public async Task Package_WithSpecialCharactersInProductNames_DisplaysCorrectly()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Name = "Café Latté", IsAlcoholic = false },
            new() { Name = "Crème Brûlée", IsAlcoholic = false },
            new() { Name = "Spaghetti & Meatballs", IsAlcoholic = false }
        };

        var package = new Package
        {
            Id = 1,
            Name = "Special Package",
            Products = products,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(2),
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            MealType = MealType.Mixed,
            Price = 5.00m
        };

        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Now.AddYears(-20),
            IdentityId = "test-id"
        };

        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(new List<Package> { package });
        
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync("test-id"))
            .ReturnsAsync(student);

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync("test-id", new PackageFilterDto());
        var displayedPackage = result.First();

        // Assert
        Assert.Equal(3, displayedPackage.ExampleProducts.Count);
        Assert.Contains("Café Latté", displayedPackage.ExampleProducts);
        Assert.Contains("Crème Brûlée", displayedPackage.ExampleProducts);
        Assert.Contains("Spaghetti & Meatballs", displayedPackage.ExampleProducts);
    }

    [Fact]
    public async Task Package_WithDuplicateProducts_DisplaysAllInstances()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Name = "Sandwich", IsAlcoholic = false },
            new() { Name = "Sandwich", IsAlcoholic = false }, // Duplicate
            new() { Name = "Water", IsAlcoholic = false }
        };

        var package = new Package
        {
            Id = 1,
            Name = "Duplicate Package",
            Products = products,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(2),
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            MealType = MealType.BreadAssortment,
            Price = 5.00m
        };

        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Now.AddYears(-20),
            IdentityId = "test-id"
        };

        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(new List<Package> { package });
        
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync("test-id"))
            .ReturnsAsync(student);

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync("test-id", new PackageFilterDto());
        var displayedPackage = result.First();

        // Assert
        Assert.Equal(3, displayedPackage.ExampleProducts.Count);
        Assert.Equal(2, displayedPackage.ExampleProducts.Count(p => p == "Sandwich"));
        Assert.Contains("Water", displayedPackage.ExampleProducts);
    }

    [Fact]
    public async Task MultiplePackages_DisplayCorrectProductsForEach()
    {
        // Arrange
        var package1 = new Package
        {
            Id = 1,
            Name = "Breakfast Package",
            Products = new List<Product> 
            { 
                new() { Name = "Coffee", IsAlcoholic = false },
                new() { Name = "Croissant", IsAlcoholic = false }
            },
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(2),
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            MealType = MealType.BreadAssortment,
            Price = 5.00m
        };

        var package2 = new Package
        {
            Id = 2,
            Name = "Lunch Package",
            Products = new List<Product> 
            { 
                new() { Name = "Sandwich", IsAlcoholic = false },
                new() { Name = "Juice", IsAlcoholic = false }
            },
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(2),
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            MealType = MealType.BreadAssortment,
            Price = 5.00m
        };

        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Now.AddYears(-20),
            IdentityId = "test-id"
        };

        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(new List<Package> { package1, package2 });
        
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync("test-id"))
            .ReturnsAsync(student);

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync("test-id", new PackageFilterDto());
        var packages = result.ToList();

        // Assert
        Assert.Equal(2, packages.Count);
        
        var breakfastPackage = packages.First(p => p.Name == "Breakfast Package");
        Assert.Equal(2, breakfastPackage.ExampleProducts.Count);
        Assert.Contains("Coffee", breakfastPackage.ExampleProducts);
        Assert.Contains("Croissant", breakfastPackage.ExampleProducts);

        var lunchPackage = packages.First(p => p.Name == "Lunch Package");
        Assert.Equal(2, lunchPackage.ExampleProducts.Count);
        Assert.Contains("Sandwich", lunchPackage.ExampleProducts);
        Assert.Contains("Juice", lunchPackage.ExampleProducts);
    }
}

