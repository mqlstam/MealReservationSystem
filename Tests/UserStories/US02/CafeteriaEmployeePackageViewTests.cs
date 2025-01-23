using System.Security.Claims;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApp.Controllers;
using WebApp.Models.Package;

namespace Tests.UserStories.US02;

public class CafeteriaEmployeePackageViewTests
{
    private readonly Mock<IPackageRepository> _mockPackageRepo;
    private readonly Mock<ICafeteriaRepository> _mockCafeteriaRepo;
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly PackageManagementController _controller;
    private readonly ApplicationUser _testEmployee;

    public CafeteriaEmployeePackageViewTests()
    {
        _mockPackageRepo = new Mock<IPackageRepository>();
        _mockCafeteriaRepo = new Mock<ICafeteriaRepository>();
        _mockStudentService = new Mock<IStudentService>();

        // Setup UserManager mock
        var mockStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockStore.Object, null, null, null, null, null, null, null, null);

        _controller = new PackageManagementController(
            _mockPackageRepo.Object,
            _mockCafeteriaRepo.Object,
            _mockStudentService.Object,
            _mockUserManager.Object);

        // Setup test employee
        _testEmployee = new ApplicationUser
        {
            Id = "test-employee-id",
            UserName = "test@employee.com",
            CafeteriaLocation = CafeteriaLocation.LA.ToString()
        };

        // Setup claims principal for authorization
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testEmployee.Id),
            new Claim(ClaimTypes.Role, "CafeteriaEmployee")
        }));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Setup default user manager behavior
        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testEmployee);
    }

    [Fact]
    public async Task Index_ReturnsViewWithAllPackages_WhenShowOnlyMyCafeteriaIsFalse()
    {
        // Arrange
        var packages = new List<Package>
        {
            CreatePackage(1, "Package LA", CafeteriaLocation.LA, DateTime.Now.AddDays(1)),
            CreatePackage(2, "Package LD", CafeteriaLocation.LD, DateTime.Now.AddDays(1)),
            CreatePackage(3, "Package DB", CafeteriaLocation.DB, DateTime.Now.AddDays(2))
        };

        _mockPackageRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(packages);

        // Act
        var result = await _controller.Index(showOnlyMyCafeteria: false) as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = Assert.IsType<PackageListViewModel>(result.Model);
        Assert.Equal(3, model.Packages.Count());
    }

    [Fact]
    public async Task Index_ReturnsViewWithFilteredPackages_WhenShowOnlyMyCafeteriaIsTrue()
    {
        // Arrange
        var packages = new List<Package>
        {
            CreatePackage(1, "Package LA 1", CafeteriaLocation.LA, DateTime.Now.AddDays(1)),
            CreatePackage(2, "Package LA 2", CafeteriaLocation.LA, DateTime.Now.AddDays(2)),
            CreatePackage(3, "Package LD", CafeteriaLocation.LD, DateTime.Now.AddDays(1))
        };

        _mockPackageRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(packages);

        // Act
        var result = await _controller.Index(showOnlyMyCafeteria: true) as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = Assert.IsType<PackageListViewModel>(result.Model);
        Assert.Equal(2, model.Packages.Count());
        Assert.All(model.Packages, p => Assert.Equal(CafeteriaLocation.LA, p.CafeteriaLocation));
    }

    [Fact]
    public async Task Index_SortsPackagesByPickupDate_Ascending()
    {
        // Arrange
        var packages = new List<Package>
        {
            CreatePackage(1, "Later Package", CafeteriaLocation.LA, DateTime.Now.AddDays(3)),
            CreatePackage(2, "Earlier Package", CafeteriaLocation.LA, DateTime.Now.AddDays(1)),
            CreatePackage(3, "Middle Package", CafeteriaLocation.LA, DateTime.Now.AddDays(2))
        };

        _mockPackageRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(packages);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = Assert.IsType<PackageListViewModel>(result.Model);
        var orderedPackages = model.Packages.ToList();
        Assert.Equal(3, orderedPackages.Count);
        Assert.Equal("Earlier Package", orderedPackages[0].Name);
        Assert.Equal("Middle Package", orderedPackages[1].Name);
        Assert.Equal("Later Package", orderedPackages[2].Name);
    }

    [Fact]
    public async Task Index_ReturnsEmptyList_WhenNoPackagesExist()
    {
        // Arrange
        _mockPackageRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Package>());

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = Assert.IsType<PackageListViewModel>(result.Model);
        Assert.Empty(model.Packages);
    }

    [Fact]
    public async Task Index_IncludesReservedPackages_InResults()
    {
        // Arrange
        var packages = new List<Package>
        {
            CreatePackage(1, "Available Package", CafeteriaLocation.LA, DateTime.Now.AddDays(1)),
            CreatePackage(2, "Reserved Package", CafeteriaLocation.LA, DateTime.Now.AddDays(1), true)
        };

        _mockPackageRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(packages);

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = Assert.IsType<PackageListViewModel>(result.Model);
        Assert.Equal(2, model.Packages.Count());
        Assert.Contains(model.Packages, p => p.IsReserved);
    }

    [Fact]
    public async Task Index_ReturnsChallenge_WhenUserNotAuthenticated()
    {
        // Arrange
        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser)null);

        // Act
        var result = await _controller.Index();

        // Assert
        Assert.IsType<ChallengeResult>(result);
    }

    [Fact]
    public async Task Index_HandlesInvalidCafeteriaLocation_GracefullyReturningAllPackages()
    {
        // Arrange
        _testEmployee.CafeteriaLocation = "InvalidLocation";
        var packages = new List<Package>
        {
            CreatePackage(1, "Package LA", CafeteriaLocation.LA, DateTime.Now.AddDays(1)),
            CreatePackage(2, "Package LD", CafeteriaLocation.LD, DateTime.Now.AddDays(1))
        };

        _mockPackageRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(packages);

        // Act
        var result = await _controller.Index(showOnlyMyCafeteria: true) as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = Assert.IsType<PackageListViewModel>(result.Model);
        Assert.Equal(2, model.Packages.Count());
    }

    [Fact]
    public async Task Index_PreservesPackageDetails_InViewModel()
    {
        // Arrange
        var package = CreatePackage(1, "Test Package", CafeteriaLocation.LA, DateTime.Now.AddDays(1));
        package.Price = 10.99m;
        package.MealType = MealType.HotMeal;
        package.IsAdultOnly = true;
        package.Products = new List<Product>
        {
            new() { Name = "Product 1" },
            new() { Name = "Product 2" }
        };

        _mockPackageRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Package> { package });

        // Act
        var result = await _controller.Index() as ViewResult;

        // Assert
        Assert.NotNull(result);
        var model = Assert.IsType<PackageListViewModel>(result.Model);
        var viewModel = model.Packages.First();
        
        Assert.Equal(package.Id, viewModel.Id);
        Assert.Equal(package.Name, viewModel.Name);
        Assert.Equal(package.CafeteriaLocation, viewModel.CafeteriaLocation);
        Assert.Equal(package.Price, viewModel.Price);
        Assert.Equal(package.MealType, viewModel.MealType);
        Assert.Equal(package.IsAdultOnly, viewModel.IsAdultOnly);
        Assert.Equal(2, viewModel.Products.Count);
    }

    private Package CreatePackage(
        int id, 
        string name, 
        CafeteriaLocation location, 
        DateTime pickupDateTime, 
        bool isReserved = false)
    {
        var package = new Package
        {
            Id = id,
            Name = name,
            City = City.Breda,
            CafeteriaLocation = location,
            PickupDateTime = pickupDateTime,
            LastReservationDateTime = pickupDateTime.AddHours(-2),
            Price = 5.00m,
            MealType = MealType.Mixed,
            Products = new List<Product>(),
            Cafeteria = new Cafeteria { City = City.Breda, Location = location }
        };

        if (isReserved)
        {
            package.Reservation = new Reservation
            {
                StudentNumber = "test-student",
                ReservationDateTime = DateTime.Now
            };
        }

        return package;
    }
}
