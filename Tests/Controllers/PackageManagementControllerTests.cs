using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApp.Controllers;
using WebApp.Models.Package;

namespace Tests.Controllers
{
    public class PackageManagementControllerTests
    {
        private readonly Mock<IPackageRepository> _mockPackageRepo;
        private readonly Mock<ICafeteriaRepository> _mockCafeteriaRepo;
        private readonly Mock<IStudentService> _mockStudentService;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly PackageManagementController _controller;

        public PackageManagementControllerTests()
        {
            _mockPackageRepo = new Mock<IPackageRepository>();
            _mockCafeteriaRepo = new Mock<ICafeteriaRepository>();
            _mockStudentService = new Mock<IStudentService>();
            
            // Setup mock UserManager
            var mockStore = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockStore.Object, null, null, null, null, null, null, null, null);

            _controller = new PackageManagementController(
                _mockPackageRepo.Object,
                _mockCafeteriaRepo.Object,
                _mockStudentService.Object,
                _mockUserManager.Object);
        }

        private Package CreateTestPackage(int id, string name, CafeteriaLocation location, City city, DateTime pickupTime, decimal price = 5.0m)
        {
            return new Package
            {
                Id = id,
                Name = name,
                CafeteriaLocation = location,
                City = city,
                PickupDateTime = pickupTime,
                LastReservationDateTime = pickupTime.AddHours(-2),
                Price = price,
                Products = new List<Product>(),
                MealType = MealType.Mixed,
                Cafeteria = new Cafeteria 
                { 
                    City = city,
                    Location = location
                }
            };
        }

        [Fact]
        public async Task Index_ReturnsViewWithAllPackages_WhenShowOnlyMyCafeteriaIsFalse()
        {
            // Arrange
            var employee = new ApplicationUser
            {
                Id = "test-id",
                CafeteriaLocation = CafeteriaLocation.LA.ToString()
            };

            var packages = new List<Package>
            {
                CreateTestPackage(1, "Package LA", CafeteriaLocation.LA, City.Breda, DateTime.Now.AddDays(1)),
                CreateTestPackage(2, "Package LD", CafeteriaLocation.LD, City.Breda, DateTime.Now.AddDays(1))
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(employee);
            _mockPackageRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(packages);

            // Act
            var result = await _controller.Index(showOnlyMyCafeteria: false) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            Assert.Equal(2, model.Packages.Count());
            Assert.Contains(model.Packages, p => p.CafeteriaLocation == CafeteriaLocation.LA);
            Assert.Contains(model.Packages, p => p.CafeteriaLocation == CafeteriaLocation.LD);
        }

        [Fact]
        public async Task Index_ReturnsViewWithFilteredPackages_WhenShowOnlyMyCafeteriaIsTrue()
        {
            // Arrange
            var employee = new ApplicationUser
            {
                Id = "test-id",
                CafeteriaLocation = CafeteriaLocation.LA.ToString()
            };

            var packages = new List<Package>
            {
                CreateTestPackage(1, "Package LA", CafeteriaLocation.LA, City.Breda, DateTime.Now.AddDays(1)),
                CreateTestPackage(2, "Package LD", CafeteriaLocation.LD, City.Breda, DateTime.Now.AddDays(1))
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(employee);
            _mockPackageRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(packages);

            // Act
            var result = await _controller.Index(showOnlyMyCafeteria: true) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            var packageList = model.Packages.ToList();
            Assert.Single(packageList);
            Assert.Equal(CafeteriaLocation.LA, packageList[0].CafeteriaLocation);
        }

        [Fact]
        public async Task Index_FiltersExpiredPackages_WhenShowExpiredIsFalse()
        {
            // Arrange
            var employee = new ApplicationUser
            {
                Id = "test-id",
                CafeteriaLocation = CafeteriaLocation.LA.ToString()
            };

            var packages = new List<Package>
            {
                CreateTestPackage(1, "Active Package", CafeteriaLocation.LA, City.Breda, DateTime.Now.AddDays(1)),
                CreateTestPackage(2, "Expired Package", CafeteriaLocation.LA, City.Breda, DateTime.Now.AddDays(-1))
            };

            packages[1].LastReservationDateTime = DateTime.Now.AddDays(-2); // Ensure it's expired

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(employee);
            _mockPackageRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(packages);

            // Act
            var result = await _controller.Index(showExpired: false) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            Assert.Single(model.Packages);
            Assert.Equal("Active Package", model.Packages.First().Name);
        }

        [Fact]
        public async Task Index_AppliesLocationFilter_WhenSpecified()
        {
            // Arrange
            var employee = new ApplicationUser
            {
                Id = "test-id",
                CafeteriaLocation = CafeteriaLocation.LA.ToString()
            };

            var packages = new List<Package>
            {
                CreateTestPackage(1, "Package Breda", CafeteriaLocation.LA, City.Breda, DateTime.Now.AddDays(1)),
                CreateTestPackage(2, "Package Den Bosch", CafeteriaLocation.DB, City.DenBosch, DateTime.Now.AddDays(1))
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(employee);
            _mockPackageRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(packages);

            // Act
            var result = await _controller.Index(cityFilter: City.Breda) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            var filteredPackages = model.Packages.ToList();
            Assert.Single(filteredPackages);
            Assert.Equal(City.Breda, filteredPackages[0].City);
        }

        [Fact]
        public async Task Index_AppliesPriceFilter_WhenSpecified()
        {
            // Arrange
            var employee = new ApplicationUser
            {
                Id = "test-id",
                CafeteriaLocation = CafeteriaLocation.LA.ToString()
            };

            var packages = new List<Package>
            {
                CreateTestPackage(1, "Cheap Package", CafeteriaLocation.LA, City.Breda, DateTime.Now.AddDays(1), 5.00m),
                CreateTestPackage(2, "Expensive Package", CafeteriaLocation.LA, City.Breda, DateTime.Now.AddDays(1), 15.00m)
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(employee);
            _mockPackageRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(packages);

            // Act
            var result = await _controller.Index(maxPrice: 10.00m) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            var filteredPackages = model.Packages.ToList();
            Assert.Single(filteredPackages);
            Assert.Equal("Cheap Package", filteredPackages[0].Name);
            Assert.True(filteredPackages[0].Price <= 10.00m);
        }

        [Fact]
        public async Task Index_ReturnsChallenge_WhenUserNotFound()
        {
            // Arrange
            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.Index();

            // Assert
            Assert.IsType<ChallengeResult>(result);
        }

        [Fact]
        public async Task Index_SortsPackagesByPickupDate()
        {
            // Arrange
            var employee = new ApplicationUser
            {
                Id = "test-id",
                CafeteriaLocation = CafeteriaLocation.LA.ToString()
            };

            var packages = new List<Package>
            {
                CreateTestPackage(1, "Later Package", CafeteriaLocation.LA, City.Breda, DateTime.Now.AddDays(2)),
                CreateTestPackage(2, "Earlier Package", CafeteriaLocation.LA, City.Breda, DateTime.Now.AddDays(1))
            };

            _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(employee);
            _mockPackageRepo.Setup(x => x.GetAllAsync())
                .ReturnsAsync(packages);

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<PackageListViewModel>(result.Model);
            var packageList = model.Packages.ToList();
            Assert.Equal(2, packageList.Count);
            Assert.Equal("Earlier Package", packageList[0].Name);
            Assert.Equal("Later Package", packageList[1].Name);
        }
    }
}
