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
using Xunit;
using Application.Services.PackageManagement;
using Application.Services.PackageManagement.DTOs;
using System.Collections.Generic;
using System.Linq;
using System;
using Application.DTOs.PackageManagement;

namespace Tests.UserStories.US03
{
    public class PackageManagementTests
    {
        private readonly Mock<IPackageManagementService> _mockPackageService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly PackageManagementController _controller;
        private readonly ApplicationUser _testEmployee;

        public PackageManagementTests()
        {
            _mockPackageService = new Mock<IPackageManagementService>();

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

            _controller = new PackageManagementController(
                _mockPackageService.Object,
                _mockUserManager.Object
            )
            {
                TempData = new TempDataDictionary(
                    new DefaultHttpContext(),
                    Mock.Of<ITempDataProvider>()
                )
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, _testEmployee.Id),
                new Claim(ClaimTypes.Role, "CafeteriaEmployee")
            }));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            _mockUserManager
                .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testEmployee);
        }

        [Fact]
        public async Task Create_Success_WhenValidPackageWithinTwoDays()
        {
            var model = new CreatePackageViewModel
            {
                Name = "Test Package",
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(23),
                Price = 5.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string> { "Product 1", "Product 2" }
            };

            _mockPackageService
                .Setup(s => s.CreatePackageAsync(
                    _testEmployee.Id,
                    It.IsAny<CreatePackageDto>()
                ))
                .ReturnsAsync((true, "Package created successfully."));

            var result = await _controller.Create(model);

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
            var model = new CreatePackageViewModel
            {
                Name = "Test Package",
                PickupDateTime = DateTime.Now.AddDays(3),
                LastReservationDateTime = DateTime.Now.AddDays(2),
                Price = 5.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string> { "Product 1" }
            };

            _controller.ModelState.AddModelError("", "Packages can only be created maximum 2 days in advance");
            _mockPackageService
                .Setup(s => s.CreatePackageAsync(
                    _testEmployee.Id,
                    It.IsAny<CreatePackageDto>()
                ))
                .ReturnsAsync((false, "Packages can only be created maximum 2 days in advance"));

            var result = await _controller.Create(model);

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
            var model = new CreatePackageViewModel
            {
                Name = "Test Package",
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(23),
                Price = 5.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string> { "Product 1" }
            };

            _controller.ModelState.AddModelError("", "Package location must match your assigned location");
            _mockPackageService
                .Setup(s => s.CreatePackageAsync(_testEmployee.Id, It.IsAny<CreatePackageDto>()))
                .ReturnsAsync((false, "Package location must match your assigned location"));

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
        }

        [Fact]
        public async Task Create_Fails_WhenNoProductsProvided()
        {
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

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Contains(
                "At least one product is required",
                viewResult.ViewData.ModelState["ExampleProducts"].Errors.Select(e => e.ErrorMessage)
            );
        }

        [Fact]
        public async Task Create_Fails_WhenLastReservationAfterPickup()
        {
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

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
        }

        [Fact]
        public async Task Create_HandlesInvalidCafeteriaLocation()
        {
            var model = new CreatePackageViewModel
            {
                Name = "Test Package",
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(23),
                Price = 5.00m,
                MealType = MealType.BreadAssortment,
                ExampleProducts = new List<string> { "Product 1" }
            };

            _mockPackageService
                .Setup(s => s.CreatePackageAsync(_testEmployee.Id, It.IsAny<CreatePackageDto>()))
                .ReturnsAsync((false, "Unable to find your cafeteria location."));

            var result = await _controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
        }

        [Fact]
        public async Task Edit_Success_WhenPackageNotReserved()
        {
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

            var result = await _controller.Edit(1, model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task Edit_Fails_WhenPackageReserved()
        {
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

            var result = await _controller.Edit(1, model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Cannot edit a package that is already reserved.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Delete_Success_WhenPackageNotReserved()
        {
            _mockPackageService
                .Setup(s => s.DeletePackageAsync(1))
                .ReturnsAsync((true, "Package deleted successfully."));

            var result = await _controller.DeleteConfirmed(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _mockPackageService.Verify(s => s.DeletePackageAsync(1), Times.Once);
        }

        [Fact]
        public async Task Delete_Fails_WhenPackageReserved()
        {
            _mockPackageService
                .Setup(s => s.DeletePackageAsync(1))
                .ReturnsAsync((false, "Cannot delete a package that is already reserved."));

            var result = await _controller.DeleteConfirmed(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Cannot delete a package that is already reserved.", _controller.TempData["Error"]);
        }
    }
}
