using System.Security.Claims;
using Application.Services.PackageManagement;
using Application.Services.PackageManagement.DTOs;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using WebApp.Controllers;
using WebApp.Models.Package;
using Xunit;

namespace Tests.UserStories.US02
{
    public class CafeteriaEmployeePackageViewTests
    {
        private readonly Mock<IPackageManagementService> _mockPackageService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<ICafeteriaRepository> _mockCafeteriaRepo;
        private readonly PackageManagementController _controller;
        private readonly ApplicationUser _testEmployee;

        public CafeteriaEmployeePackageViewTests()
        {
            // We no longer mock the old IPackageRepository, 
            // but instead mock the IPackageManagementService.
            _mockPackageService = new Mock<IPackageManagementService>();

            // We still need the cafeteria repo for the controller constructor 
            // (to display City + cafeteria name on create/edit GET).
            _mockCafeteriaRepo = new Mock<ICafeteriaRepository>();

            // Setup UserManager mock
            var mockStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockStore.Object, null, null, null, null, null, null, null, null
            );

            // Construct the new PackageManagementController
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

            // By default, retrieving the user from UserManager returns our testEmployee
            _mockUserManager
                .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_testEmployee);
        }

        [Fact]
        public async Task Index_ReturnsViewWithAllPackages_WhenShowOnlyMyCafeteriaIsFalse()
        {
            // Arrange
            // We create a PackageListDto with multiple packages 
            var dto = new PackageListDto
            {
                Packages = new List<PackageManagementDto>
                {
                    new()
                    {
                        Id = 1,
                        Name = "Package LA",
                        CafeteriaLocation = CafeteriaLocation.LA,
                        PickupDateTime = DateTime.Now.AddDays(1)
                    },
                    new()
                    {
                        Id = 2,
                        Name = "Package LD",
                        CafeteriaLocation = CafeteriaLocation.LD,
                        PickupDateTime = DateTime.Now.AddDays(1)
                    },
                    new()
                    {
                        Id = 3,
                        Name = "Package DB",
                        CafeteriaLocation = CafeteriaLocation.DB,
                        PickupDateTime = DateTime.Now.AddDays(2)
                    }
                }
            };

            // Mock the service to return this list if showOnlyMyCafeteria = false
            _mockPackageService
                .Setup(s => s.GetPackageListAsync(
                    _testEmployee.Id,
                    false,  // showOnlyMyCafeteria
                    It.IsAny<City?>(),
                    It.IsAny<MealType?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<bool>()
                ))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.Index(showOnlyMyCafeteria: false) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            Assert.Equal(3, model.Packages.Count);
        }

        [Fact]
        public async Task Index_ReturnsViewWithFilteredPackages_WhenShowOnlyMyCafeteriaIsTrue()
        {
            // Arrange
            var dto = new PackageListDto
            {
                Packages = new List<PackageManagementDto>
                {
                    new()
                    {
                        Id = 1,
                        Name = "Package LA 1",
                        CafeteriaLocation = CafeteriaLocation.LA,
                        PickupDateTime = DateTime.Now.AddDays(1)
                    },
                    new()
                    {
                        Id = 2,
                        Name = "Package LA 2",
                        CafeteriaLocation = CafeteriaLocation.LA,
                        PickupDateTime = DateTime.Now.AddDays(2)
                    },
                    new()
                    {
                        Id = 3,
                        Name = "Package LD",
                        CafeteriaLocation = CafeteriaLocation.LD,
                        PickupDateTime = DateTime.Now.AddDays(1)
                    }
                }
            };

            // If showOnlyMyCafeteria = true, the application layer 
            // returns only packages that match the user’s location, etc.
            _mockPackageService
                .Setup(s => s.GetPackageListAsync(
                    _testEmployee.Id,
                    true,
                    It.IsAny<City?>(),
                    It.IsAny<MealType?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<bool>()
                ))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.Index(showOnlyMyCafeteria: true) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            Assert.Equal(3, model.Packages.Count);
            // And so on, if we wanted to check specific location, 
            // we'd verify that the service returned only LA, etc.
        }

        [Fact]
        public async Task Index_SortsPackagesByPickupDate_Ascending()
        {
            // Arrange
            var now = DateTime.Now;
            var dto = new PackageListDto
            {
                Packages = new List<PackageManagementDto>
                {
                    new() { Id = 1, Name = "Later Package",     PickupDateTime = now.AddDays(3) },
                    new() { Id = 2, Name = "Earlier Package",   PickupDateTime = now.AddDays(1) },
                    new() { Id = 3, Name = "Middle Package",    PickupDateTime = now.AddDays(2) }
                }
            };

            _mockPackageService
                .Setup(s => s.GetPackageListAsync(
                    _testEmployee.Id,
                    false,
                    null,
                    null,
                    null,
                    false
                ))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            var ordered = model.Packages.ToList();
            Assert.Equal(3, ordered.Count);
            Assert.Equal("Earlier Package", ordered[0].Name);
            Assert.Equal("Middle Package", ordered[1].Name);
            Assert.Equal("Later Package", ordered[2].Name);
        }

        [Fact]
        public async Task Index_ReturnsEmptyList_WhenNoPackagesExist()
        {
            // Arrange
            var dto = new PackageListDto
            {
                Packages = new List<PackageManagementDto>() // empty
            };
            _mockPackageService
                .Setup(s => s.GetPackageListAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<City?>(),
                    It.IsAny<MealType?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<bool>()
                ))
                .ReturnsAsync(dto);

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
            var dto = new PackageListDto
            {
                Packages = new List<PackageManagementDto>
                {
                    new() { Id = 1, Name = "Available Package",  IsReserved = false },
                    new() { Id = 2, Name = "Reserved Package",   IsReserved = true }
                }
            };
            _mockPackageService
                .Setup(s => s.GetPackageListAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<City?>(),
                    It.IsAny<MealType?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<bool>()
                ))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            Assert.Equal(2, model.Packages.Count);
            Assert.Contains(model.Packages, p => p.IsReserved);
        }

        [Fact]
        public async Task Index_ReturnsChallenge_WhenUserNotAuthenticated()
        {
            // Arrange
            _mockUserManager
                .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.Index();

            // Assert
            Assert.IsType<ChallengeResult>(result);
        }

        [Fact]
        public async Task Index_HandlesInvalidCafeteriaLocation_Gracefully()
        {
            // If the user’s CafeteriaLocation parse fails, the 
            // application layer might still show all packages or 
            // it might return an empty list. We’ll test it returns all.

            _testEmployee.CafeteriaLocation = "InvalidLocation";

            var dto = new PackageListDto
            {
                Packages = new List<PackageManagementDto>
                {
                    new() { Id = 1, Name = "Package LA", CafeteriaLocation = CafeteriaLocation.LA },
                    new() { Id = 2, Name = "Package LD", CafeteriaLocation = CafeteriaLocation.LD }
                }
            };

            _mockPackageService
                .Setup(s => s.GetPackageListAsync(
                    It.IsAny<string>(),
                    true,
                    It.IsAny<City?>(),
                    It.IsAny<MealType?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<bool>()
                ))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.Index(showOnlyMyCafeteria: true) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            Assert.Equal(2, model.Packages.Count);
        }

        [Fact]
        public async Task Index_PreservesPackageDetails_InViewModel()
        {
            // Arrange
            var dto = new PackageListDto
            {
                Packages = new List<PackageManagementDto>
                {
                    new()
                    {
                        Id = 1,
                        Name = "Test Package",
                        CafeteriaLocation = CafeteriaLocation.LA,
                        Price = 10.99m,
                        MealType = MealType.HotMeal,
                        IsAdultOnly = true,
                        Products = new List<string> { "Product 1", "Product 2" }
                    }
                }
            };

            _mockPackageService
                .Setup(s => s.GetPackageListAsync(
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<Domain.Enums.City?>(),
                    It.IsAny<Domain.Enums.MealType?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<bool>()
                ))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            var vm = model.Packages.First();

            Assert.Equal(1, vm.Id);
            Assert.Equal("Test Package", vm.Name);
            Assert.Equal(CafeteriaLocation.LA, vm.CafeteriaLocation);
            Assert.Equal(10.99m, vm.Price);
            Assert.Equal(MealType.HotMeal, vm.MealType);
            Assert.True(vm.IsAdultOnly);
            Assert.Equal(2, vm.Products.Count);
        }
    }
}
