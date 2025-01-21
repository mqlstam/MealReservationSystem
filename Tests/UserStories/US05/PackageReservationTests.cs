using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tests.Helpers;

namespace Tests.UserStories.US05;

public class PackageReservationTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPackageRepository> _mockPackageRepo;
    private readonly Mock<IReservationRepository> _mockReservationRepo;
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly Mock<INoShowService> _mockNoShowService;
    private readonly Mock<IAgeVerificationService> _mockAgeVerificationService;
    private readonly ReservationService _reservationService;

    public PackageReservationTests()
    {
        _context = TestDbContext.Create();
        _mockPackageRepo = new Mock<IPackageRepository>();
        _mockReservationRepo = new Mock<IReservationRepository>();
        _mockStudentService = new Mock<IStudentService>();
        _mockNoShowService = new Mock<INoShowService>();
        _mockAgeVerificationService = new Mock<IAgeVerificationService>();

        _reservationService = new ReservationService(
            _mockPackageRepo.Object,
            _mockReservationRepo.Object,
            _mockStudentService.Object,
            _mockNoShowService.Object,
            _mockAgeVerificationService.Object
        );
    }

    [Fact]
    public async Task ReservePackage_Success_WhenAllConditionsAreMet()
    {
        // Arrange
        var student = CreateTestStudent();
        var package = CreateTestPackage();

        SetupMocksForSuccessfulReservation(student, package);

        // Act
        var result = await _reservationService.ReservePackageAsync(package.Id, student.IdentityId);

        // Assert
        Assert.Equal("Package reserved successfully!", result);
        _mockReservationRepo.Verify(
            x => x.AddAsync(It.IsAny<Reservation>()), 
            Times.Once);
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenStudentNotFound()
    {
        // Arrange
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Student?)null);

        // Act
        var result = await _reservationService.ReservePackageAsync(1, "non-existent-id");

        // Assert
        Assert.Equal("Student record not found", result);
        _mockReservationRepo.Verify(
            x => x.AddAsync(It.IsAny<Reservation>()), 
            Times.Never);
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenPackageNotFound()
    {
        // Arrange
        var student = CreateTestStudent();
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(It.IsAny<string>()))
            .ReturnsAsync(student);
        _mockPackageRepo.Setup(p => p.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Package?)null);

        // Act
        var result = await _reservationService.ReservePackageAsync(1, student.IdentityId);

        // Assert
        Assert.Equal("Package not found", result);
        _mockReservationRepo.Verify(
            x => x.AddAsync(It.IsAny<Reservation>()), 
            Times.Never);
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenPackageAlreadyReserved()
    {
        // Arrange
        var student = CreateTestStudent();
        var package = CreateTestPackage();
        package.Reservation = new Reservation(); // Package is already reserved

        SetupBasicMocks(student, package);

        // Act
        var result = await _reservationService.ReservePackageAsync(package.Id, student.IdentityId);

        // Assert
        Assert.Equal("Package already reserved", result);
        _mockReservationRepo.Verify(
            x => x.AddAsync(It.IsAny<Reservation>()), 
            Times.Never);
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenPickupTimePassed()
    {
        // Arrange
        var student = CreateTestStudent();
        var package = CreateTestPackage();
        package.PickupDateTime = DateTime.Now.AddHours(-1); // Pickup time in the past

        SetupBasicMocks(student, package);

        // Act
        var result = await _reservationService.ReservePackageAsync(package.Id, student.IdentityId);

        // Assert
        Assert.Equal("Pickup time has passed", result);
        _mockReservationRepo.Verify(
            x => x.AddAsync(It.IsAny<Reservation>()), 
            Times.Never);
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenReservationDeadlinePassed()
    {
        // Arrange
        var student = CreateTestStudent();
        var package = CreateTestPackage();
        package.LastReservationDateTime = DateTime.Now.AddHours(-1); // Reservation deadline passed

        SetupBasicMocks(student, package);

        // Act
        var result = await _reservationService.ReservePackageAsync(package.Id, student.IdentityId);

        // Assert
        Assert.Equal("Reservation time has passed", result);
        _mockReservationRepo.Verify(
            x => x.AddAsync(It.IsAny<Reservation>()), 
            Times.Never);
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenStudentHasTooManyNoShows()
    {
        // Arrange
        var student = CreateTestStudent();
        var package = CreateTestPackage();

        SetupBasicMocks(student, package);
        _mockNoShowService.Setup(x => x.CanStudentReserveAsync(student.IdentityId))
            .ReturnsAsync(false);

        // Act
        var result = await _reservationService.ReservePackageAsync(package.Id, student.IdentityId);

        // Assert
        Assert.Equal("You cannot make reservations due to multiple no-shows", result);
        _mockReservationRepo.Verify(
            x => x.AddAsync(It.IsAny<Reservation>()), 
            Times.Never);
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenStudentUnderageForAdultPackage()
    {
        // Arrange
        var student = CreateTestStudent(age: 17);
        var package = CreateTestPackage(isAdultOnly: true);

        SetupBasicMocks(student, package);
        _mockAgeVerificationService.Setup(x => x.IsStudentEligibleForPackage(student, package))
            .Returns(false);

        // Act
        var result = await _reservationService.ReservePackageAsync(package.Id, student.IdentityId);

        // Assert
        Assert.Equal("You must be 18 or older to reserve this package", result);
        _mockReservationRepo.Verify(
            x => x.AddAsync(It.IsAny<Reservation>()), 
            Times.Never);
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenStudentAlreadyHasReservationForDate()
    {
        // Arrange
        var student = CreateTestStudent();
        var package = CreateTestPackage();

        SetupBasicMocks(student, package);
        _mockReservationRepo.Setup(x => x.HasReservationForDateAsync(
                student.IdentityId, 
                package.PickupDateTime.Date))
            .ReturnsAsync(true);

        // Act
        var result = await _reservationService.ReservePackageAsync(package.Id, student.IdentityId);

        // Assert
        Assert.Equal("You already have a reservation for this date", result);
        _mockReservationRepo.Verify(
            x => x.AddAsync(It.IsAny<Reservation>()), 
            Times.Never);
    }

    [Fact]
    public async Task ReservePackage_HandlesRaceCondition_WhenPackageReservedConcurrently()
    {
        // Arrange
        var student = CreateTestStudent();
        var package = CreateTestPackage();

        SetupMocksForSuccessfulReservation(student, package);
        _mockReservationRepo.Setup(x => x.AddAsync(It.IsAny<Reservation>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());

        // Act
        var result = await _reservationService.ReservePackageAsync(package.Id, student.IdentityId);

        // Assert
        Assert.Equal("Package already reserved", result);
    }

    private Student CreateTestStudent(int age = 20)
    {
        return new Student
        {
            StudentNumber = "2024001",
            Email = "test@test.com",
            FirstName = "Test",
            LastName = "Student",
            DateOfBirth = DateTime.Now.AddYears(-age),
            StudyCity = City.Breda,
            IdentityId = "test-identity-id"
        };
    }

    private Package CreateTestPackage(bool isAdultOnly = false)
    {
        return new Package
        {
            Id = 1,
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            IsAdultOnly = isAdultOnly,
            Price = 5.00m,
            MealType = MealType.Mixed
        };
    }

    private void SetupBasicMocks(Student student, Package package)
    {
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(student.IdentityId))
            .ReturnsAsync(student);
        _mockPackageRepo.Setup(p => p.GetByIdAsync(package.Id))
            .ReturnsAsync(package);
        _mockNoShowService.Setup(x => x.CanStudentReserveAsync(student.IdentityId))
            .ReturnsAsync(true);
        _mockAgeVerificationService.Setup(x => x.IsStudentEligibleForPackage(student, package))
            .Returns(true);
        _mockReservationRepo.Setup(x => x.HasReservationForDateAsync(
                student.IdentityId, 
                package.PickupDateTime.Date))
            .ReturnsAsync(false);
    }

    private void SetupMocksForSuccessfulReservation(Student student, Package package)
    {
        SetupBasicMocks(student, package);
        _mockReservationRepo.Setup(x => x.AddAsync(It.IsAny<Reservation>()))
            .ReturnsAsync(new Reservation());
    }
}
