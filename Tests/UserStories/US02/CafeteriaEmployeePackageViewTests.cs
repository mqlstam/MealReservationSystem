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
        private readonly PackageManagementController _controller;
        private readonly ApplicationUser _testEmployee;

        public CafeteriaEmployeePackageViewTests()
        {
            _mockPackageService = new Mock<IPackageManagementService>();

            var mockStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockStore.Object, null, null, null, null, null, null, null, null
            );

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

            _testEmployee = new ApplicationUser
            {
                Id = "test-employee-id",
                UserName = "test@employee.com",
                CafeteriaLocation = CafeteriaLocation.LA.ToString()
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
        public async Task Index_ReturnsViewWithAllPackages_WhenShowOnlyMyCafeteriaIsFalse()
        {
            var dto = new PackageListDto
            {
                Packages = new List<PackageManagementDto>
                {
                    new() { Id = 1, Name = "Package LA", CafeteriaLocation = CafeteriaLocation.LA },
                    new() { Id = 2, Name = "Package LD", CafeteriaLocation = CafeteriaLocation.LD },
                    new() { Id = 3, Name = "Package DB", CafeteriaLocation = CafeteriaLocation.DB }
                }
            };

            _mockPackageService
                .Setup(s => s.GetPackageListAsync(
                    _testEmployee.Id,
                    false,
                    It.IsAny<City?>(),
                    It.IsAny<MealType?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<bool>()
                ))
                .ReturnsAsync(dto);

            var result = await _controller.Index(showOnlyMyCafeteria: false) as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            Assert.Equal(3, model.Packages.Count);
        }

        [Fact]
        public async Task Index_ReturnsViewWithFilteredPackages_WhenShowOnlyMyCafeteriaIsTrue()
        {
            var dto = new PackageListDto
            {
                Packages = new List<PackageManagementDto>
                {
                    new() { Id = 1, Name = "Package LA 1", CafeteriaLocation = CafeteriaLocation.LA },
                    new() { Id = 2, Name = "Package LA 2", CafeteriaLocation = CafeteriaLocation.LA },
                    new() { Id = 3, Name = "Package LD",   CafeteriaLocation = CafeteriaLocation.LD }
                }
            };

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

            var result = await _controller.Index(showOnlyMyCafeteria: true) as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            Assert.Equal(3, model.Packages.Count);
        }

        [Fact]
        public async Task Index_SortsPackagesByPickupDate_Ascending()
        {
            var now = DateTime.Now;
            var dto = new PackageListDto
            {
                Packages = new List<PackageManagementDto>
                {
                    new() { Id = 1, Name = "Later Package",   PickupDateTime = now.AddDays(3) },
                    new() { Id = 2, Name = "Earlier Package", PickupDateTime = now.AddDays(1) },
                    new() { Id = 3, Name = "Middle Package",  PickupDateTime = now.AddDays(2) }
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

            var result = await _controller.Index() as ViewResult;

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
            var dto = new PackageListDto
            {
                Packages = new List<PackageManagementDto>()
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

            var result = await _controller.Index() as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            Assert.Empty(model.Packages);
        }

        [Fact]
        public async Task Index_IncludesReservedPackages_InResults()
        {
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

            var result = await _controller.Index() as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            Assert.Equal(2, model.Packages.Count);
            Assert.Contains(model.Packages, p => p.IsReserved);
        }

        [Fact]
        public async Task Index_ReturnsChallenge_WhenUserNotAuthenticated()
        {
            _mockUserManager
                .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((ApplicationUser)null);

            var result = await _controller.Index();

            Assert.IsType<ChallengeResult>(result);
        }

        [Fact]
        public async Task Index_HandlesInvalidCafeteriaLocation_Gracefully()
        {
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

            var result = await _controller.Index(showOnlyMyCafeteria: true) as ViewResult;

            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            Assert.Equal(2, model.Packages.Count);
        }

        [Fact]
        public async Task Index_PreservesPackageDetails_InViewModel()
        {
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
                    It.IsAny<City?>(),
                    It.IsAny<MealType?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<bool>()
                ))
                .ReturnsAsync(dto);

            var result = await _controller.Index() as ViewResult;

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
