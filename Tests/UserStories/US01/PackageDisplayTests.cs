using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.DTOs.Common;
using Application.DTOs.Packages;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Tests.UserStories.US01;

public class PackageDisplayTests
{
    private readonly Mock<IPackageRepository> _mockPackageRepo;
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly Mock<IReservationRepository> _mockReservationRepo;
    private readonly IPackageViewService _packageViewService;
    private readonly string _testStudentId = "test-student-id";

    public PackageDisplayTests()
    {
        _mockPackageRepo = new Mock<IPackageRepository>();
        _mockStudentService = new Mock<IStudentService>();
        _mockReservationRepo = new Mock<IReservationRepository>();
        
        _packageViewService = new PackageViewService(
            _mockPackageRepo.Object,
            _mockStudentService.Object,
            _mockReservationRepo.Object);

        // Default setup for no-shows and date reservations
        _mockReservationRepo.Setup(r => r.HasReservationForDateAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(false);
        _mockReservationRepo.Setup(r => r.GetNoShowCountAsync(It.IsAny<string>()))
            .ReturnsAsync(0);
    }

    private Student CreateTestStudent(bool isAdult = true)
    {
        return new Student
        {
            StudentNumber = "123456",
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "Student",
            DateOfBirth = isAdult ? DateTime.Now.AddYears(-20) : DateTime.Now.AddYears(-17),
            StudyCity = City.Breda,
            IdentityId = _testStudentId
        };
    }

    private Package CreateTestPackage(
        int id,
        string name,
        bool isExpired = false,
        bool isReserved = false,
        bool isAdultOnly = false)
    {
        var package = new Package
        {
            Id = id,
            Name = name,
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = isExpired ? DateTime.Now.AddDays(-1) : DateTime.Now.AddDays(1),
            LastReservationDateTime = isExpired ? DateTime.Now.AddDays(-2) : DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.Mixed,
            Products = new List<Product>
            {
                new() { Name = "Test Product 1", IsAlcoholic = isAdultOnly },
                new() { Name = "Test Product 2" }
            }
        };

        if (isReserved)
        {
            package.Reservation = new Reservation
            {
                Id = 1,
                StudentNumber = "123456",
                ReservationDateTime = DateTime.Now
            };
        }

        return package;
    }

    [Fact]
    public async Task GetAvailablePackages_ReturnsOnlyNonReservedAndNonExpiredPackages()
    {
        // Arrange
        var availablePackage = CreateTestPackage(1, "Available Package");
        var reservedPackage = CreateTestPackage(2, "Reserved Package", isReserved: true);
        var expiredPackage = CreateTestPackage(3, "Expired Package", isExpired: true);

        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testStudentId))
            .ReturnsAsync(CreateTestStudent());

        // Only return the available package from the repository
        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(new List<Package> { availablePackage });

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testStudentId, new PackageFilterDto());

        // Assert
        Assert.Single(result);
        Assert.Equal("Available Package", result.First().Name);
    }

    [Fact]
    public async Task GetAvailablePackages_WhenStudentIsMinor_HidesAdultOnlyPackages()
    {
        // Arrange
        var regularPackage = CreateTestPackage(1, "Regular Package");
        var adultPackage = CreateTestPackage(2, "Adult Package", isAdultOnly: true);

        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testStudentId))
            .ReturnsAsync(CreateTestStudent(isAdult: false));

        // Return both packages from repository
        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(new List<Package> { regularPackage });

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testStudentId, new PackageFilterDto());

        // Assert
        var availablePackages = result.ToList();
        Assert.Single(availablePackages);
        Assert.Equal("Regular Package", availablePackages[0].Name);
        Assert.False(availablePackages[0].IsAdultOnly);
    }
    
    [Fact]
    public async Task GetStudentReservations_ReturnsAllReservationsIncludingExpired()
    {
        // Arrange
        var reservations = new List<Reservation>
        {
            new()
            {
                Id = 1,
                Package = CreateTestPackage(1, "Current Package"),
                StudentNumber = "123456",
                ReservationDateTime = DateTime.Now
            },
            new()
            {
                Id = 2,
                Package = CreateTestPackage(2, "Past Package", isExpired: true),
                StudentNumber = "123456",
                ReservationDateTime = DateTime.Now.AddDays(-2)
            }
        };

        _mockReservationRepo.Setup(r => r.GetByStudentIdAsync(_testStudentId))
            .ReturnsAsync(reservations);

        // Act
        var result = await _packageViewService.GetStudentReservationsAsync(_testStudentId);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.Contains(result, r => r.PackageName == "Current Package");
        Assert.Contains(result, r => r.PackageName == "Past Package");
    }

    [Fact]
    public async Task GetAvailablePackages_WithNoPackagesAvailable_ReturnsEmptyList()
    {
        // Arrange
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testStudentId))
            .ReturnsAsync(CreateTestStudent());
        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(new List<Package>());

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testStudentId, new PackageFilterDto());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailablePackages_WithInvalidStudent_ReturnsEmptyList()
    {
        // Arrange
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testStudentId))
            .ReturnsAsync((Student)null);

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testStudentId, new PackageFilterDto());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetStudentReservations_WithNoReservations_ReturnsEmptyList()
    {
        // Arrange
        _mockReservationRepo.Setup(r => r.GetByStudentIdAsync(_testStudentId))
            .ReturnsAsync(new List<Reservation>());

        // Act
        var result = await _packageViewService.GetStudentReservationsAsync(_testStudentId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailablePackages_PreservesPackageDetails()
    {
        // Arrange
        var package = CreateTestPackage(1, "Test Package");
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testStudentId))
            .ReturnsAsync(CreateTestStudent());
        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(new List<Package> { package });

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testStudentId, new PackageFilterDto());
        var resultPackage = result.First();

        // Assert
        Assert.Equal(package.Id, resultPackage.Id);
        Assert.Equal(package.Name, resultPackage.Name);
        Assert.Equal(package.City, resultPackage.City);
        Assert.Equal(package.CafeteriaLocation, resultPackage.CafeteriaLocation);
        Assert.Equal(package.PickupDateTime, resultPackage.PickupDateTime);
        Assert.Equal(package.LastReservationDateTime, resultPackage.LastReservationDateTime);
        Assert.Equal(package.Price, resultPackage.Price);
        Assert.Equal(package.MealType, resultPackage.MealType);
        Assert.Equal(2, resultPackage.ExampleProducts.Count);
    }

    [Fact]
    public async Task GetStudentReservations_WithNoShowStatus_ReturnsCorrectStatus()
    {
        // Arrange
        var reservations = new List<Reservation>
        {
            new()
            {
                Id = 1,
                Package = CreateTestPackage(1, "NoShow Package"),
                StudentNumber = "123456",
                ReservationDateTime = DateTime.Now.AddDays(-2),
                IsNoShow = true
            },
            new()
            {
                Id = 2,
                Package = CreateTestPackage(2, "Picked Up Package"),
                StudentNumber = "123456",
                ReservationDateTime = DateTime.Now.AddDays(-1),
                IsPickedUp = true
            }
        };

        _mockReservationRepo.Setup(r => r.GetByStudentIdAsync(_testStudentId))
            .ReturnsAsync(reservations);

        // Act
        var result = await _packageViewService.GetStudentReservationsAsync(_testStudentId);
        var resultList = result.ToList();

        // Assert
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, r => r.IsNoShow);
        Assert.Contains(resultList, r => r.IsPickedUp);
    }

    [Fact]
    public async Task GetAvailablePackages_WithNoShowLimit_BlocksNewReservations()
    {
        // Arrange
        var packages = new List<Package> { CreateTestPackage(1, "Test Package") };
        
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testStudentId))
            .ReturnsAsync(CreateTestStudent());
        _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
            .ReturnsAsync(packages);
        _mockReservationRepo.Setup(r => r.GetNoShowCountAsync(_testStudentId))
            .ReturnsAsync(2); // No-show limit reached

        // Act
        var result = await _packageViewService.GetAvailablePackagesAsync(_testStudentId, new PackageFilterDto());
        var package = result.First();

        // Assert
        Assert.False(package.CanReserve);
        Assert.Equal("You cannot make reservations due to multiple no-shows.", package.ReservationBlockReason);
    }
}
