using System.Security.Claims;
using Domain.Enums;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using WebApp.Controllers;
using WebApp.Models.Package;
using Xunit;
using Application.Services.PackageManagement;
using Application.Services.PackageManagement.DTOs;

namespace Tests.UserStories.US03
{
    public class PackageManagementTests
    {
        // Instead of mocking repositories directly, we now mock the IPackageManagementService
        // because that's what the new PackageManagementController uses in the 'new way'.
        private readonly Mock<IPackageManagementService> _mockPackageService;
        private readonly Mock<ICafeteriaRepository> _mockCafeteriaRepo;
        private readonly Mock<IStudentService> _mockStudentService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly PackageManagementController _controller;
        private readonly ApplicationUser _testEmployee;
        private readonly Cafeteria _testCafeteria;

        public PackageManagementTests()
        {
            _mockPackageService = new Mock<IPackageManagementService>();
            _mockCafeteriaRepo = new Mock<ICafeteriaRepository>();
            _mockStudentService = new Mock<IStudentService>();

            var mockStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockStore.Object, null, null, null, null, null, null, null, null
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

            // By default, retrieving the user from UserManager returns our _testEmployee
            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testEmployee);

            // By default, the cafeteria repo returns the test cafeteria
            _mockCafeteriaRepo.Setup(x => x.GetByLocationAsync(CafeteriaLocation.LA))
                .ReturnsAsync(_testCafeteria);

            // Construct controller
            _controller = new PackageManagementController(
                _mockPackageService.Object,
                _mockUserManager.Object,
                _mockCafeteriaRepo.Object
            )
            {
                TempData = new TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<ITempDataProvider>()
                )
            };

            // Setup controller context with claims
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
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(23),
                Price = 5.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string> { "Product 1", "Product 2" }
            };

            // We mock the application service to indicate success
            _mockPackageService
                .Setup(s => s.CreatePackageAsync(
                    _testEmployee.Id,
                    It.IsAny<CreatePackageDto>()
                ))
                .ReturnsAsync((true, "Package created successfully."));

            // Act
            var result = await _controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            _mockPackageService.Verify(s =>
                s.CreatePackageAsync(_testEmployee.Id, It.IsAny<CreatePackageDto>()),
                Times.Once
            );
        }

        [Fact]
        public async Task Create_Fails_WhenPickupDateMoreThanTwoDaysAhead()
        {
            // Arrange
            var model = new CreatePackageViewModel
            {
                Name = "Test Package",
                PickupDateTime = DateTime.Now.AddDays(3),
                LastReservationDateTime = DateTime.Now.AddDays(2),
                Price = 5.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string> { "Product 1" }
            };

            // We force an error in ModelState (the "two days in advance" rule)
            _controller.ModelState.AddModelError("", "Packages can only be created maximum 2 days in advance");

            // We also mock the service to return false
            _mockPackageService
                .Setup(s => s.CreatePackageAsync(
                    _testEmployee.Id,
                    It.IsAny<CreatePackageDto>()
                ))
                .ReturnsAsync((false, "Packages can only be created maximum 2 days in advance"));

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);

            Assert.Contains(
                "Packages can only be created maximum 2 days in advance",
                viewResult.ViewData.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
            );
        }

        [Fact]
        public async Task Create_Fails_WhenLocationMismatch()
        {
            // Arrange
            var model = new CreatePackageViewModel
            {
                Name = "Test Package",
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(23),
                Price = 5.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string> { "Product 1" }
            };

            // Suppose the service fails because the location mismatched
            _controller.ModelState.AddModelError("", "Package location must match your assigned location");
            _mockPackageService
                .Setup(s => s.CreatePackageAsync(_testEmployee.Id, It.IsAny<CreatePackageDto>()))
                .ReturnsAsync((false, "Package location must match your assigned location"));

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
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(23),
                Price = 5.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string>()
            };

            _controller.ModelState.AddModelError("ExampleProducts", "At least one product is required");
            _mockPackageService
                .Setup(s => s.CreatePackageAsync(_testEmployee.Id, It.IsAny<CreatePackageDto>()))
                .ReturnsAsync((false, "At least one product is required"));

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Contains(
                "At least one product is required",
                viewResult.ViewData.ModelState["ExampleProducts"].Errors.Select(e => e.ErrorMessage)
            );
        }

        [Fact]
        public async Task Create_Fails_WhenLastReservationAfterPickup()
        {
            // Arrange
            var model = new CreatePackageViewModel
            {
                Name = "Test Package",
                PickupDateTime = DateTime.Now.AddHours(1),
                LastReservationDateTime = DateTime.Now.AddHours(2),
                Price = 5.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string> { "Product 1" }
            };

            _controller.ModelState.AddModelError("LastReservationDateTime", "Last reservation time must be before pickup time");
            _mockPackageService
                .Setup(s => s.CreatePackageAsync(_testEmployee.Id, It.IsAny<CreatePackageDto>()))
                .ReturnsAsync((false, "Last reservation time must be before pickup time"));

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
            _mockCafeteriaRepo
                .Setup(x => x.GetByLocationAsync(CafeteriaLocation.LA))
                .ReturnsAsync((Cafeteria)null);

            var model = new CreatePackageViewModel
            {
                Name = "Test Package",
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(23),
                Price = 5.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string> { "Product 1" }
            };

            // Suppose the service also fails
            _mockPackageService
                .Setup(s => s.CreatePackageAsync(_testEmployee.Id, It.IsAny<CreatePackageDto>()))
                .ReturnsAsync((false, "Unable to find your cafeteria location."));

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
        }

        [Fact]
        public async Task Edit_Success_WhenPackageNotReserved()
        {
            // Suppose the "edit" GET call was successful, and for the POST we want success
            _mockPackageService
                .Setup(s => s.UpdatePackageAsync(
                    1,
                    It.IsAny<CreatePackageDto>(),
                    _testEmployee.Id
                ))
                .ReturnsAsync((true, "Package updated successfully."));

            var model = new CreatePackageViewModel
            {
                Name = "Updated Package",
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(23),
                Price = 6.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string> { "Updated Product" }
            };

            // Act
            var result = await _controller.Edit(1, model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Edit_Fails_WhenPackageReserved()
        {
            // Suppose the application layer says "Cannot edit a package that is reserved"
            _mockPackageService
                .Setup(s => s.UpdatePackageAsync(
                    1,
                    It.IsAny<CreatePackageDto>(),
                    _testEmployee.Id
                ))
                .ReturnsAsync((false, "Cannot edit a package that is already reserved."));

            var model = new CreatePackageViewModel
            {
                Name = "Updated Package",
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
            // Suppose the service says the package is successfully deleted
            _mockPackageService
                .Setup(s => s.DeletePackageAsync(1))
                .ReturnsAsync((true, "Package deleted successfully."));

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            // We can verify the call
            _mockPackageService.Verify(s => s.DeletePackageAsync(1), Times.Once);
        }

        [Fact]
        public async Task Delete_Fails_WhenPackageReserved()
        {
            // Suppose the service says "Cannot delete a package that is reserved"
            _mockPackageService
                .Setup(s => s.DeletePackageAsync(1))
                .ReturnsAsync((false, "Cannot delete a package that is already reserved."));

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Cannot delete a package that is already reserved.", _controller.TempData["Error"]);
        }
    }
}
