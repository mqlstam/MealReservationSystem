using System.Security.Claims;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using WebApp.Controllers;
using WebApp.Models.Package;

namespace Tests.UserStories.US03;

public class PackageManagementTests
{
    private readonly Mock<IPackageRepository> _mockPackageRepo;
    private readonly Mock<ICafeteriaRepository> _mockCafeteriaRepo;
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly PackageManagementController _controller;
    private readonly ApplicationUser _testEmployee;
    private readonly Cafeteria _testCafeteria;

    public PackageManagementTests()
    {
        _mockPackageRepo = new Mock<IPackageRepository>();
        _mockCafeteriaRepo = new Mock<ICafeteriaRepository>();
        _mockStudentService = new Mock<IStudentService>();
        
        var mockStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockStore.Object,
            null, null, null, null, null, null, null, null
        );

        _testEmployee = new ApplicationUser
        {
            Id = "test-employee-id",
            UserName = "test@employee.com",
            CafeteriaLocation = CafeteriaLocation.LA.ToString()
        };

        _testCafeteria = new Cafeteria
        {
            Id = 1,
            City = City.Breda,
            Location = CafeteriaLocation.LA,
            OffersHotMeals = true
        };

        // Setup default mocks
        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testEmployee);

        _mockCafeteriaRepo.Setup(x => x.GetByLocationAsync(CafeteriaLocation.LA))
            .ReturnsAsync(_testCafeteria);

        // Setup controller with TempData
        _controller = new PackageManagementController(
            _mockPackageRepo.Object,
            _mockCafeteriaRepo.Object,
            _mockStudentService.Object,
            _mockUserManager.Object)
        {
            TempData = new TempDataDictionary(
                new DefaultHttpContext(), 
                Mock.Of<ITempDataProvider>()
            )
        };

        // Setup controller context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testEmployee.Id),
            new Claim(ClaimTypes.Role, "CafeteriaEmployee")
        }));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task Create_Success_WhenValidPackageWithinTwoDays()
    {
        // Arrange
        var model = new CreatePackageViewModel
        {
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            ExampleProducts = new List<string> { "Product 1", "Product 2" }
        };

        _mockPackageRepo.Setup(x => x.AddAsync(It.IsAny<Package>()))
            .ReturnsAsync((Package p) => 
            {
                p.Id = 1;
                p.Cafeteria = _testCafeteria;
                return p;
            });

        // Act
        var result = await _controller.Create(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _mockPackageRepo.Verify(x => x.AddAsync(It.IsAny<Package>()), Times.Once);
    }

    [Fact]
    public async Task Create_Fails_WhenPickupDateMoreThanTwoDaysAhead()
    {
        // Arrange
        var model = new CreatePackageViewModel
        {
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(3),
            LastReservationDateTime = DateTime.Now.AddDays(2),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            ExampleProducts = new List<string> { "Product 1" }
        };

        _controller.ModelState.AddModelError("", "Packages can only be created maximum 2 days in advance");

        // Act
        var result = await _controller.Create(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var modelState = viewResult.ViewData.ModelState;
        Assert.False(modelState.IsValid);
        Assert.Contains(modelState.Values, v => 
            v.Errors.Any(e => e.ErrorMessage == "Packages can only be created maximum 2 days in advance"));
    }

    [Fact]
    public async Task Create_Fails_WhenLocationMismatch()
    {
        // Arrange
        var model = new CreatePackageViewModel
        {
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LD,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            ExampleProducts = new List<string> { "Product 1" }
        };

        _controller.ModelState.AddModelError("", "Package location must match your assigned location");

        // Act
        var result = await _controller.Create(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(viewResult.ViewData.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_Fails_WhenNoProductsProvided()
    {
        // Arrange
        var model = new CreatePackageViewModel
        {
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            ExampleProducts = new List<string>()
        };

        _controller.ModelState.AddModelError("ExampleProducts", "At least one product is required");

        // Act
        var result = await _controller.Create(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Contains("At least one product is required", 
            viewResult.ViewData.ModelState["ExampleProducts"].Errors.Select(e => e.ErrorMessage));
    }

    [Fact]
    public async Task Create_Fails_WhenLastReservationAfterPickup()
    {
        // Arrange
        var model = new CreatePackageViewModel
        {
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddHours(1),
            LastReservationDateTime = DateTime.Now.AddHours(2),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            ExampleProducts = new List<string> { "Product 1" }
        };

        _controller.ModelState.AddModelError("LastReservationDateTime", "Last reservation time must be before pickup time");

        // Act
        var result = await _controller.Create(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(viewResult.ViewData.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_HandlesInvalidCafeteriaLocation()
    {
        // Arrange
        _mockCafeteriaRepo.Setup(x => x.GetByLocationAsync(CafeteriaLocation.LA))
            .ReturnsAsync((Cafeteria)null);

        var model = new CreatePackageViewModel
        {
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            ExampleProducts = new List<string> { "Product 1" }
        };

        // Act
        var result = await _controller.Create(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(viewResult.ViewData.ModelState.IsValid);
    }

    [Fact]
    public async Task Edit_Success_WhenPackageNotReserved()
    {
        // Arrange
        var existingPackage = new Package
        {
            Id = 1,
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            Products = new List<Product> { new() { Name = "Product 1" } },
            CafeteriaId = _testCafeteria.Id,
            Cafeteria = _testCafeteria
        };

        _mockPackageRepo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existingPackage);

        var model = new CreatePackageViewModel
        {
            Name = "Updated Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 6.00m,
            MealType = MealType.BreadAssortment,
            ExampleProducts = new List<string> { "Updated Product" }
        };

        _mockPackageRepo.Setup(x => x.UpdateAsync(It.IsAny<Package>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Edit(1, model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task Edit_Fails_WhenPackageReserved()
    {
        // Arrange
        var existingPackage = new Package
        {
            Id = 1,
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            Reservation = new Reservation { StudentNumber = "123456" },
            CafeteriaId = _testCafeteria.Id,
            Cafeteria = _testCafeteria
        };

        _mockPackageRepo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existingPackage);

        var model = new CreatePackageViewModel
        {
            Name = "Updated Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            ExampleProducts = new List<string> { "Product 1" }
        };

        // Act
        var result = await _controller.Edit(1, model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Cannot edit a package that is already reserved.", _controller.TempData["Error"]);
    }

    [Fact]
    public async Task Delete_Success_WhenPackageNotReserved()
    {
        // Arrange
        var package = new Package
        {
            Id = 1,
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            Reservation = null,
            CafeteriaId = _testCafeteria.Id,
            Cafeteria = _testCafeteria
        };

        _mockPackageRepo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(package);
        
        _mockPackageRepo.Setup(x => x.DeleteAsync(1))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _mockPackageRepo.Verify(x => x.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task Delete_Fails_WhenPackageReserved()
    {
        // Arrange
        var package = new Package
        {
            Id = 1,
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            Reservation = new Reservation { StudentNumber = "123456" },
            CafeteriaId = _testCafeteria.Id,
            Cafeteria = _testCafeteria
        };

        _mockPackageRepo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(package);

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("Cannot delete a package that is already reserved.", _controller.TempData["Error"]);
    }
}
