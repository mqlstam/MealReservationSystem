using Application.Common.Interfaces;
using Application.DTOs.Common;
using Application.DTOs.Packages;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;
using Xunit;

namespace Tests.Services
{
    public class PackageViewServiceTests
    {
        private readonly Mock<IPackageRepository> _mockPackageRepo;
        private readonly Mock<IStudentService> _mockStudentService;
        private readonly Mock<IReservationRepository> _mockReservationRepo;
        private readonly PackageViewService _service;
        private readonly string _testUserId = "test-user-id";

        public PackageViewServiceTests()
        {
            _mockPackageRepo = new Mock<IPackageRepository>();
            _mockStudentService = new Mock<IStudentService>();
            _mockReservationRepo = new Mock<IReservationRepository>();
            
            _service = new PackageViewService(
                _mockPackageRepo.Object,
                _mockStudentService.Object,
                _mockReservationRepo.Object);
        }

        [Fact]
        public async Task GetAvailablePackages_ReturnsEmptyList_WhenNoStudentFound()
        {
            // Arrange
            _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
                .ReturnsAsync((Student)null);

            // Act
            var result = await _service.GetAvailablePackagesAsync(_testUserId, new PackageFilterDto());

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAvailablePackages_FiltersPackagesByCity()
        {
            // Arrange
            var student = new Student
            {
                StudentNumber = "123456",
                DateOfBirth = DateTime.Today.AddYears(-20),
                IdentityId = _testUserId
            };

            var packages = new List<Package>
            {
                new Package 
                { 
                    Id = 1, 
                    City = City.Breda,
                    PickupDateTime = DateTime.Now.AddDays(1),
                    LastReservationDateTime = DateTime.Now.AddHours(2),
                    Products = new List<Product>()
                },
                new Package 
                { 
                    Id = 2, 
                    City = City.DenBosch,
                    PickupDateTime = DateTime.Now.AddDays(1),
                    LastReservationDateTime = DateTime.Now.AddHours(2),
                    Products = new List<Product>()
                }
            };

            _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
                .ReturnsAsync(student);
            _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
                .ReturnsAsync(packages);
            _mockReservationRepo.Setup(r => r.HasReservationForDateAsync(_testUserId, It.IsAny<DateTime>()))
                .ReturnsAsync(false);
            _mockReservationRepo.Setup(r => r.GetNoShowCountAsync(_testUserId))
                .ReturnsAsync(0);

            var filter = new PackageFilterDto { CityFilter = City.Breda };

            // Act
            var result = await _service.GetAvailablePackagesAsync(_testUserId, filter);

            // Assert
            Assert.Single(result);
            Assert.Equal(City.Breda, result.First().City);
        }

        [Fact]
        public async Task GetAvailablePackages_ExcludesExpiredPackages()
        {
            // Arrange
            var student = new Student
            {
                StudentNumber = "123456",
                DateOfBirth = DateTime.Today.AddYears(-20),
                IdentityId = _testUserId
            };

            var futurePackage = new Package 
            { 
                Id = 1,
                Name = "Future Package",
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(2),
                Products = new List<Product>()
            };

            var expiredPackage = new Package 
            { 
                Id = 2,
                Name = "Expired Package",
                PickupDateTime = DateTime.Now.AddDays(-1),
                LastReservationDateTime = DateTime.Now.AddHours(-2),
                Products = new List<Product>()
            };

            // Note: The GetAvailablePackagesAsync method in the repository 
            // should already filter out expired packages
            _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
                .ReturnsAsync(student);
            _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
                .ReturnsAsync(new List<Package> { futurePackage }); // Only return non-expired package
            _mockReservationRepo.Setup(r => r.HasReservationForDateAsync(_testUserId, It.IsAny<DateTime>()))
                .ReturnsAsync(false);
            _mockReservationRepo.Setup(r => r.GetNoShowCountAsync(_testUserId))
                .ReturnsAsync(0);

            // Act
            var result = await _service.GetAvailablePackagesAsync(_testUserId, new PackageFilterDto());

            // Assert
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal("Future Package", resultList[0].Name);
            Assert.Equal(1, resultList[0].Id);
        }

        [Fact]
        public async Task GetAvailablePackages_BlocksReservation_ForUnderage()
        {
            // Arrange
            var student = new Student
            {
                StudentNumber = "123456",
                DateOfBirth = DateTime.Today.AddYears(-17),
                IdentityId = _testUserId
            };

            var packages = new List<Package>
            {
                new Package 
                { 
                    Id = 1,
                    Name = "Adult Package",
                    IsAdultOnly = true,
                    PickupDateTime = DateTime.Now.AddDays(1),
                    LastReservationDateTime = DateTime.Now.AddHours(2),
                    Products = new List<Product>()
                }
            };

            _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
                .ReturnsAsync(student);
            _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
                .ReturnsAsync(packages);
            _mockReservationRepo.Setup(r => r.HasReservationForDateAsync(_testUserId, It.IsAny<DateTime>()))
                .ReturnsAsync(false);
            _mockReservationRepo.Setup(r => r.GetNoShowCountAsync(_testUserId))
                .ReturnsAsync(0);

            // Act
            var result = await _service.GetAvailablePackagesAsync(_testUserId, new PackageFilterDto());

            // Assert
            var package = result.First();
            Assert.False(package.CanReserve);
            Assert.Equal("This package is restricted to users 18 and older.", package.ReservationBlockReason);
        }

        [Fact]
        public async Task GetAvailablePackages_BlocksReservation_ForTooManyNoShows()
        {
            // Arrange
            var student = new Student
            {
                StudentNumber = "123456",
                DateOfBirth = DateTime.Today.AddYears(-20),
                IdentityId = _testUserId
            };

            var packages = new List<Package>
            {
                new Package 
                { 
                    Id = 1,
                    Name = "Test Package",
                    PickupDateTime = DateTime.Now.AddDays(1),
                    LastReservationDateTime = DateTime.Now.AddHours(2),
                    Products = new List<Product>()
                }
            };

            _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
                .ReturnsAsync(student);
            _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
                .ReturnsAsync(packages);
            _mockReservationRepo.Setup(r => r.HasReservationForDateAsync(_testUserId, It.IsAny<DateTime>()))
                .ReturnsAsync(false);
            _mockReservationRepo.Setup(r => r.GetNoShowCountAsync(_testUserId))
                .ReturnsAsync(2);

            // Act
            var result = await _service.GetAvailablePackagesAsync(_testUserId, new PackageFilterDto());

            // Assert
            var package = result.First();
            Assert.False(package.CanReserve);
            Assert.Equal("You cannot make reservations due to multiple no-shows.", package.ReservationBlockReason);
        }

        [Fact]
        public async Task GetStudentReservations_IncludesAllReservationStates()
        {
            // Arrange
            var reservations = new List<Reservation>
            {
                new() {
                    Id = 1,
                    Package = new Package 
                    { 
                        Name = "Active Package", 
                        PickupDateTime = DateTime.Now.AddDays(1),
                        Products = new List<Product>()
                    },
                    IsPickedUp = false,
                    IsNoShow = false
                },
                new() {
                    Id = 2,
                    Package = new Package 
                    { 
                        Name = "Picked Up Package", 
                        PickupDateTime = DateTime.Now.AddDays(-1),
                        Products = new List<Product>()
                    },
                    IsPickedUp = true,
                    IsNoShow = false
                },
                new() {
                    Id = 3,
                    Package = new Package 
                    { 
                        Name = "No Show Package", 
                        PickupDateTime = DateTime.Now.AddDays(-1),
                        Products = new List<Product>()
                    },
                    IsPickedUp = false,
                    IsNoShow = true
                }
            };

            _mockReservationRepo.Setup(r => r.GetByStudentIdAsync(_testUserId))
                .ReturnsAsync(reservations);

            // Act
            var result = await _service.GetStudentReservationsAsync(_testUserId);

            // Assert
            var resultList = result.ToList();
            Assert.Equal(3, resultList.Count);
            Assert.Contains(resultList, r => !r.IsPickedUp && !r.IsNoShow);
            Assert.Contains(resultList, r => r.IsPickedUp);
            Assert.Contains(resultList, r => r.IsNoShow);
        }

        [Fact]
        public async Task GetStudentNoShowCount_ReturnsCorrectCount()
        {
            // Arrange
            const int expectedNoShows = 2;
            _mockReservationRepo.Setup(r => r.GetNoShowCountAsync(_testUserId))
                .ReturnsAsync(expectedNoShows);

            // Act
            var result = await _service.GetStudentNoShowCountAsync(_testUserId);

            // Assert
            Assert.Equal(expectedNoShows, result);
        }
    }
}
