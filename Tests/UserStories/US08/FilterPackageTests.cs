using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.DTOs.Common;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tests.Helpers;

namespace Tests.UserStories.US08;

public class FilterPackageTests
{
    private readonly Mock<IPackageRepository> _mockPackageRepo;
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly Mock<IReservationRepository> _mockReservationRepo;
    private readonly IPackageViewService _packageViewService;
    private readonly string _testUserId = "test-user-id";

    public FilterPackageTests()
    {
        _mockPackageRepo = new Mock<IPackageRepository>();
        _mockStudentService = new Mock<IStudentService>();
        _mockReservationRepo = new Mock<IReservationRepository>();
        
        _packageViewService = new PackageViewService(
            _mockPackageRepo.Object,
            _mockStudentService.Object,
            _mockReservationRepo.Object);
        
        // Setup common mocks
        _mockReservationRepo.Setup(r => r.HasReservationForDateAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(false);
        _mockReservationRepo.Setup(r => r.GetNoShowCountAsync(It.IsAny<string>()))
            .ReturnsAsync(0);
    }

    [Fact]
    public async Task FilterPackages_ReturnAllPackages_WhenNoFiltersApplied()
    {
        // Arrange
        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Today.AddYears(-20),
            IdentityId = _testUserId,
            StudyCity = City.Breda
        };

        var packages = new List<Package>
        {
            CreatePackage(1, "Package 1", City.Breda, MealType.BreadAssortment, 5.00m),
            CreatePackage(2, "Package 2", City.DenBosch, MealType.HotMeal, 7.50m),
            CreatePackage(3, "Package 3", City.Tilburg, MealType.Mixed, 6.00m)
        };

        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
            .ReturnsAsync(student);
        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(packages);

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testUserId, new PackageFilterDto());

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task FilterPackages_ReturnsCityFilteredPackages_WhenCityFilterApplied()
    {
        // Arrange
        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Today.AddYears(-20),
            IdentityId = _testUserId,
            StudyCity = City.Breda
        };

        var packages = new List<Package>
        {
            CreatePackage(1, "Package 1", City.Breda, MealType.BreadAssortment, 5.00m),
            CreatePackage(2, "Package 2", City.DenBosch, MealType.HotMeal, 7.50m),
            CreatePackage(3, "Package 3", City.Breda, MealType.Mixed, 6.00m)
        };

        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
            .ReturnsAsync(student);
        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(packages);

        var filter = new PackageFilterDto { CityFilter = City.Breda };

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testUserId, filter);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, package => Assert.Equal(City.Breda, package.City));
    }

    [Fact]
    public async Task FilterPackages_ReturnsMealTypeFilteredPackages_WhenMealTypeFilterApplied()
    {
        // Arrange
        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Today.AddYears(-20),
            IdentityId = _testUserId,
            StudyCity = City.Breda
        };

        var packages = new List<Package>
        {
            CreatePackage(1, "Package 1", City.Breda, MealType.BreadAssortment, 5.00m),
            CreatePackage(2, "Package 2", City.DenBosch, MealType.HotMeal, 7.50m),
            CreatePackage(3, "Package 3", City.Breda, MealType.BreadAssortment, 6.00m)
        };

        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
            .ReturnsAsync(student);
        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(packages);

        var filter = new PackageFilterDto { TypeFilter = MealType.BreadAssortment };

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testUserId, filter);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, package => Assert.Equal(MealType.BreadAssortment, package.MealType));
    }

    [Fact]
    public async Task FilterPackages_ReturnsPriceFilteredPackages_WhenMaxPriceFilterApplied()
    {
        // Arrange
        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Today.AddYears(-20),
            IdentityId = _testUserId,
            StudyCity = City.Breda
        };

        var packages = new List<Package>
        {
            CreatePackage(1, "Package 1", City.Breda, MealType.BreadAssortment, 5.00m),
            CreatePackage(2, "Package 2", City.DenBosch, MealType.HotMeal, 7.50m),
            CreatePackage(3, "Package 3", City.Breda, MealType.Mixed, 6.00m)
        };

        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
            .ReturnsAsync(student);
        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(packages);

        var filter = new PackageFilterDto { MaxPriceFilter = 6.00m };

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testUserId, filter);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, package => Assert.True(package.Price <= 6.00m));
    }

    [Fact]
    public async Task FilterPackages_ReturnsFilteredPackages_WhenAllFiltersApplied()
    {
        // Arrange
        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Today.AddYears(-20),
            IdentityId = _testUserId,
            StudyCity = City.Breda
        };

        var packages = new List<Package>
        {
            CreatePackage(1, "Package 1", City.Breda, MealType.BreadAssortment, 5.00m),
            CreatePackage(2, "Package 2", City.DenBosch, MealType.HotMeal, 7.50m),
            CreatePackage(3, "Package 3", City.Breda, MealType.BreadAssortment, 6.00m),
            CreatePackage(4, "Package 4", City.Breda, MealType.BreadAssortment, 8.00m)
        };

        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
            .ReturnsAsync(student);
        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(packages);

        var filter = new PackageFilterDto
        {
            CityFilter = City.Breda,
            TypeFilter = MealType.BreadAssortment,
            MaxPriceFilter = 6.00m
        };

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testUserId, filter);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, package => 
        {
            Assert.Equal(City.Breda, package.City);
            Assert.Equal(MealType.BreadAssortment, package.MealType);
            Assert.True(package.Price <= 6.00m);
        });
    }

    [Fact]
    public async Task FilterPackages_ReturnsEmptyList_WhenNoPackagesMatchFilters()
    {
        // Arrange
        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Today.AddYears(-20),
            IdentityId = _testUserId,
            StudyCity = City.Breda
        };

        var packages = new List<Package>
        {
            CreatePackage(1, "Package 1", City.DenBosch, MealType.HotMeal, 7.50m),
            CreatePackage(2, "Package 2", City.Tilburg, MealType.Mixed, 8.00m)
        };

        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
            .ReturnsAsync(student);
        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(packages);

        var filter = new PackageFilterDto
        {
            CityFilter = City.Breda,
            TypeFilter = MealType.BreadAssortment,
            MaxPriceFilter = 5.00m
        };

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testUserId, filter);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task FilterPackages_ReturnsEmptyList_WhenStudentNotFound()
    {
        // Arrange
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
            .ReturnsAsync((Student)null);

        var filter = new PackageFilterDto
        {
            CityFilter = City.Breda,
            TypeFilter = MealType.BreadAssortment
        };

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testUserId, filter);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task FilterPackages_ReturnsEmptyList_WhenNoPackagesAvailable()
    {
        // Arrange
        var student = new Student
        {
            StudentNumber = "123456",
            DateOfBirth = DateTime.Today.AddYears(-20),
            IdentityId = _testUserId,
            StudyCity = City.Breda
        };

        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
            .ReturnsAsync(student);
        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(new List<Package>());

        var filter = new PackageFilterDto
        {
            CityFilter = City.Breda,
            TypeFilter = MealType.BreadAssortment
        };

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testUserId, filter);

        // Assert
        Assert.Empty(result);
    }

    private Package CreatePackage(int id, string name, City city, MealType mealType, decimal price)
    {
        return new Package
        {
            Id = id,
            Name = name,
            City = city,
            CafeteriaLocation = CafeteriaLocation.LA, // Default test location
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(2),
            Price = price,
            MealType = mealType,
            Products = new List<Product>(),
            Cafeteria = new Cafeteria { City = city, Location = CafeteriaLocation.LA }
        };
    }
}
