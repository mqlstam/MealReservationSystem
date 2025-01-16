using Application.Common.Interfaces;
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

namespace Tests.Controllers
{
    public class PackageManagementCreateEditTests
    {
        private readonly Mock<IPackageRepository> _mockPackageRepo;
        private readonly Mock<ICafeteriaRepository> _mockCafeteriaRepo;
        private readonly Mock<IStudentService> _mockStudentService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly PackageManagementController _controller;

        public PackageManagementCreateEditTests()
        {
            _mockPackageRepo = new Mock<IPackageRepository>();
            _mockCafeteriaRepo = new Mock<ICafeteriaRepository>();
            _mockStudentService = new Mock<IStudentService>();
            
            var mockStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockStore.Object, null, null, null, null, null, null, null, null);

            _controller = new PackageManagementController(
                _mockPackageRepo.Object,
                _mockCafeteriaRepo.Object,
                _mockStudentService.Object,
                _mockUserManager.Object);

            // Setup TempData
            var tempData = new TempDataDictionary(
                new DefaultHttpContext(), 
                Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        [Fact]
        public async Task Create_Get_ReturnsViewWithModel_WhenUserIsValid()
        {
            // Arrange
            var employee = new ApplicationUser
            {
                Id = "test-id",
                CafeteriaLocation = CafeteriaLocation.LA.ToString()
            };

            var cafeteria = new Cafeteria
            {
                Id = 1,
                City = City.Breda,
                Location = CafeteriaLocation.LA,
                OffersHotMeals = true
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(employee);
            _mockCafeteriaRepo.Setup(x => x.GetByLocationAsync(CafeteriaLocation.LA))
                .ReturnsAsync(cafeteria);

            // Act
            var result = await _controller.Create() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<CreatePackageViewModel>(result.Model);
            Assert.Equal(City.Breda, model.City);
            Assert.Equal(CafeteriaLocation.LA, model.CafeteriaLocation);
        }

        [Fact]
        public async Task Create_Post_RedirectsToIndex_WhenModelIsValid()
        {
            // Arrange
            var employee = new ApplicationUser
            {
                Id = "test-id",
                CafeteriaLocation = CafeteriaLocation.LA.ToString()
            };

            var cafeteria = new Cafeteria
            {
                Id = 1,
                City = City.Breda,
                Location = CafeteriaLocation.LA,
                OffersHotMeals = true
            };

            var model = new CreatePackageViewModel
            {
                Name = "Test Package",
                City = City.Breda,
                CafeteriaLocation = CafeteriaLocation.LA,
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(22),
                Price = 5.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string> { "Product 1" }
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(employee);
            _mockCafeteriaRepo.Setup(x => x.GetByLocationAsync(CafeteriaLocation.LA))
                .ReturnsAsync(cafeteria);
            _mockPackageRepo.Setup(x => x.AddAsync(It.IsAny<Package>()))
                .ReturnsAsync(new Package { Id = 1 });

            // Act
            var result = await _controller.Create(model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockPackageRepo.Verify(x => x.AddAsync(It.IsAny<Package>()), Times.Once);
            Assert.Equal("Package created successfully.", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task Create_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            // Arrange
            var model = new CreatePackageViewModel(); // Empty model will be invalid
            _controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var result = await _controller.Create(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.False(result.ViewData.ModelState.IsValid);
            _mockPackageRepo.Verify(x => x.AddAsync(It.IsAny<Package>()), Times.Never);
        }

        [Fact]
        public async Task Edit_Get_ReturnsNotFound_WhenPackageDoesNotExist()
        {
            // Arrange
            _mockPackageRepo.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync((Package)null);

            // Act
            var result = await _controller.Edit(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Package not found.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Edit_Get_ReturnsNotFound_WhenPackageIsReserved()
        {
            // Arrange
            var package = new Package
            {
                Id = 1,
                Reservation = new Reservation()
            };

            _mockPackageRepo.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(package);

            // Act
            var result = await _controller.Edit(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Contains("already reserved", _controller.TempData["Error"].ToString());
        }

        [Fact]
        public async Task Edit_Post_UpdatesPackage_WhenModelIsValid()
        {
            // Arrange
            var employee = new ApplicationUser
            {
                Id = "test-id",
                CafeteriaLocation = CafeteriaLocation.LA.ToString()
            };

            var existingPackage = new Package
            {
                Id = 1,
                Name = "Original Name",
                Reservation = null
            };

            var model = new CreatePackageViewModel
            {
                Name = "Updated Name",
                City = City.Breda,
                CafeteriaLocation = CafeteriaLocation.LA,
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(22),
                Price = 5.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string> { "Product 1" }
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(employee);
            _mockPackageRepo.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(existingPackage);
            _mockPackageRepo.Setup(x => x.UpdateAsync(It.IsAny<Package>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Edit(1, model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockPackageRepo.Verify(x => x.UpdateAsync(It.Is<Package>(p => p.Name == "Updated Name")), Times.Once);
            Assert.Equal("Package updated successfully.", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task DeleteConfirmed_DeletesPackage_WhenNotReserved()
        {
            // Arrange
            var package = new Package
            {
                Id = 1,
                Name = "Test Package",
                Reservation = null
            };

            _mockPackageRepo.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(package);
            _mockPackageRepo.Setup(x => x.DeleteAsync(1))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteConfirmed(1) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockPackageRepo.Verify(x => x.DeleteAsync(1), Times.Once);
            Assert.Equal("Package deleted successfully.", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task DeleteConfirmed_ReturnsForbidden_WhenPackageIsReserved()
        {
            // Arrange
            var package = new Package
            {
                Id = 1,
                Name = "Test Package",
                Reservation = new Reservation()
            };

            _mockPackageRepo.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(package);

            // Act
            var result = await _controller.DeleteConfirmed(1) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            _mockPackageRepo.Verify(x => x.DeleteAsync(It.IsAny<int>()), Times.Never);
            Assert.Contains("already reserved", _controller.TempData["Error"].ToString());
        }

        [Fact]
        public async Task Create_Post_ValidatesHotMealRestriction()
        {
            // Arrange
            var employee = new ApplicationUser
            {
                Id = "test-id",
                CafeteriaLocation = CafeteriaLocation.LA.ToString()
            };

            var cafeteria = new Cafeteria
            {
                Id = 1,
                City = City.Breda,
                Location = CafeteriaLocation.LA,
                OffersHotMeals = false // This location doesn't offer hot meals
            };

            var model = new CreatePackageViewModel
            {
                Name = "Test Hot Meal",
                City = City.Breda,
                CafeteriaLocation = CafeteriaLocation.LA,
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(22),
                Price = 5.00m,
                MealType = MealType.HotMeal, // Trying to create a hot meal
                ExampleProducts = new List<string> { "Product 1" }
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(employee);
            _mockCafeteriaRepo.Setup(x => x.GetByLocationAsync(CafeteriaLocation.LA))
                .ReturnsAsync(cafeteria);

            // Act
            var result = await _controller.Create(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.False(result.ViewData.ModelState.IsValid);
            Assert.Contains(result.ViewData.ModelState["MealType"].Errors,
                error => error.ErrorMessage.Contains("does not offer hot meals"));
            _mockPackageRepo.Verify(x => x.AddAsync(It.IsAny<Package>()), Times.Never);
        }
    }
}
