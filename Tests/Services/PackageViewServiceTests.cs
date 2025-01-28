using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.DTOs.Common;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Moq;

namespace Tests.Services
{
    public class PackageViewServiceTests
    {
        private readonly Mock<IPackageRepository> _mockPackageRepo;
        private readonly Mock<IStudentService> _mockStudentService;
        private readonly Mock<IReservationRepository> _mockReservationRepo;
        private readonly PackageViewService _packageViewService;
        private readonly string _testUserId = "test-user-id";

        public PackageViewServiceTests()
        {
            _mockPackageRepo = new Mock<IPackageRepository>();
            _mockStudentService = new Mock<IStudentService>();
            _mockReservationRepo = new Mock<IReservationRepository>();

            _packageViewService = new PackageViewService(
                _mockPackageRepo.Object,
                _mockStudentService.Object,
                _mockReservationRepo.Object);

            SetupDefaultMocks();
        }

        private void SetupDefaultMocks()
        {
            _mockReservationRepo.Setup(r => r.HasReservationForDateAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                .ReturnsAsync(false);
            _mockReservationRepo.Setup(r => r.GetNoShowCountAsync(It.IsAny<string>()))
                .ReturnsAsync(0);
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

            var package = new Package 
            { 
                Id = 1,
                Name = "Adult Package",
                PickupDateTime = DateTime.Now.AddDays(1),
                LastReservationDateTime = DateTime.Now.AddHours(2),
                Products = new List<Product> { new Product { Name = "Beer", IsAlcoholic = true } }
            };
            package.UpdateIsAdultOnly();

            _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(_testUserId))
                .ReturnsAsync(student);
            _mockPackageRepo.Setup(r => r.GetAvailablePackagesAsync())
                .ReturnsAsync(new List<Package> { package });

            // Act
            var result = await _packageViewService.GetAvailablePackagesAsync(_testUserId, new PackageFilterDto());
            var resultPackage = result.First();

            // Assert
            Assert.False(resultPackage.CanReserve);
            Assert.Equal("This package is restricted to users 18 and older.", resultPackage.ReservationBlockReason);
        }
    }
}
