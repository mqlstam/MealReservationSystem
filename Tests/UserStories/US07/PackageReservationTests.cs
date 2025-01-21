using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tests.Helpers;

namespace Tests.UserStories.US07;

public class PackageReservationTests
{
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly Mock<INoShowService> _mockNoShowService;
    private readonly Mock<IAgeVerificationService> _mockAgeVerificationService;
    private readonly ApplicationDbContext _context;
    private readonly ReservationService _reservationService;
    private readonly Student _testStudent;
    private readonly Package _testPackage;

    public PackageReservationTests()
    {
        _context = TestDbContext.Create();
        _mockStudentService = new Mock<IStudentService>();
        _mockNoShowService = new Mock<INoShowService>();
        _mockAgeVerificationService = new Mock<IAgeVerificationService>();

        var packageRepository = new PackageRepository(_context);
        var reservationRepository = new ReservationRepository(_context);

        _reservationService = new ReservationService(
            packageRepository,
            reservationRepository,
            _mockStudentService.Object,
            _mockNoShowService.Object,
            _mockAgeVerificationService.Object);

        // Set up test student
        _testStudent = new Student
        {
            StudentNumber = "123456",
            FirstName = "Test",
            LastName = "Student",
            Email = "test@test.com",
            DateOfBirth = new DateTime(2000, 1, 1),
            StudyCity = City.Breda,
            IdentityId = "test-identity-id"
        };

        // Set up test package
        var cafeteria = new Cafeteria
        {
            City = City.Breda,
            Location = CafeteriaLocation.LA,
            OffersHotMeals = true
        };
        _context.Cafeterias.Add(cafeteria);
        _context.SaveChanges();

        _testPackage = new Package
        {
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            CafeteriaId = cafeteria.Id
        };

        // Configure mock services
        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(It.IsAny<string>()))
            .ReturnsAsync(_testStudent);
        _mockNoShowService.Setup(s => s.CanStudentReserveAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockAgeVerificationService.Setup(s => s.IsStudentEligibleForPackage(It.IsAny<Student>(), It.IsAny<Package>()))
            .Returns(true);
    }

    [Fact]
    public async Task ReservePackage_Success_WhenPackageIsAvailable()
    {
        // Arrange
        _context.Packages.Add(_testPackage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reservationService.ReservePackageAsync(_testPackage.Id, _testStudent.IdentityId);

        // Assert
        Assert.Equal("Package reserved successfully!", result);
        var package = await _context.Packages
            .Include(p => p.Reservation)
            .FirstOrDefaultAsync(p => p.Id == _testPackage.Id);
        Assert.NotNull(package?.Reservation);
        Assert.Equal(_testStudent.StudentNumber, package.Reservation.StudentNumber);
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenPackageAlreadyReserved()
    {
        // Arrange
        _testPackage.Reservation = new Reservation
        {
            StudentNumber = "another-student",
            ReservationDateTime = DateTime.Now
        };
        _context.Packages.Add(_testPackage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reservationService.ReservePackageAsync(_testPackage.Id, _testStudent.IdentityId);

        // Assert
        Assert.Equal("Package already reserved", result);
    }

    [Fact]
    public async Task ReservePackage_HandlesRaceCondition_WhenMultipleSimultaneousReservations()
    {
        // Arrange
        _context.Packages.Add(_testPackage);
        await _context.SaveChangesAsync();

        // Create a second student
        var student2 = new Student
        {
            StudentNumber = "789012",
            FirstName = "Second",
            LastName = "Student",
            Email = "second@test.com",
            DateOfBirth = new DateTime(2000, 1, 1),
            StudyCity = City.Breda,
            IdentityId = "second-test-id"
        };

        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync("second-test-id"))
            .ReturnsAsync(student2);

        // Act - Simulate concurrent reservations
        var reservation1Task = _reservationService.ReservePackageAsync(_testPackage.Id, _testStudent.IdentityId);
        var reservation2Task = _reservationService.ReservePackageAsync(_testPackage.Id, "second-test-id");

        var results = await Task.WhenAll(reservation1Task, reservation2Task);

        // Assert
        Assert.True(results.Count(r => r == "Package reserved successfully!") == 1);
        Assert.True(results.Count(r => r == "Package already reserved") == 1);

        var package = await _context.Packages
            .Include(p => p.Reservation)
            .FirstOrDefaultAsync(p => p.Id == _testPackage.Id);
        Assert.NotNull(package?.Reservation);
        Assert.True(
            package.Reservation.StudentNumber == _testStudent.StudentNumber ||
            package.Reservation.StudentNumber == student2.StudentNumber
        );
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenPackageDoesNotExist()
    {
        // Act
        var result = await _reservationService.ReservePackageAsync(999, _testStudent.IdentityId);

        // Assert
        Assert.Equal("Package not found", result);
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenStudentNotFound()
    {
        // Arrange
        _context.Packages.Add(_testPackage);
        await _context.SaveChangesAsync();

        _mockStudentService.Setup(s => s.GetStudentByIdentityIdAsync(It.IsAny<string>()))
            .ReturnsAsync((Student)null);

        // Act
        var result = await _reservationService.ReservePackageAsync(_testPackage.Id, "non-existent-id");

        // Assert
        Assert.Equal("Student record not found", result);
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenReservationTimeHasPassed()
    {
        // Arrange
        _testPackage.LastReservationDateTime = DateTime.Now.AddHours(-1);
        _context.Packages.Add(_testPackage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reservationService.ReservePackageAsync(_testPackage.Id, _testStudent.IdentityId);

        // Assert
        Assert.Equal("Reservation time has passed", result);
    }

    [Fact]
    public async Task ReservePackage_Fails_WhenPickupTimeHasPassed()
    {
        // Arrange
        _testPackage.PickupDateTime = DateTime.Now.AddHours(-1);
        _context.Packages.Add(_testPackage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _reservationService.ReservePackageAsync(_testPackage.Id, _testStudent.IdentityId);

        // Assert
        Assert.Equal("Pickup time has passed", result);
    }

    [Fact]
    public async Task ReservePackage_PreservesOriginalReservation_WhenConcurrentUpdatesOccur()
    {
        // Arrange
        _context.Packages.Add(_testPackage);
        await _context.SaveChangesAsync();

        // Make the first reservation
        var firstReservationResult = await _reservationService.ReservePackageAsync(_testPackage.Id, _testStudent.IdentityId);
        Assert.Equal("Package reserved successfully!", firstReservationResult);

        // Try to modify the package while it's already reserved
        var package = await _context.Packages.FindAsync(_testPackage.Id);
        package.Name = "Modified Name";
        await _context.SaveChangesAsync();

        // Verify the reservation is still intact
        var updatedPackage = await _context.Packages
            .Include(p => p.Reservation)
            .FirstOrDefaultAsync(p => p.Id == _testPackage.Id);

        Assert.NotNull(updatedPackage?.Reservation);
        Assert.Equal(_testStudent.StudentNumber, updatedPackage.Reservation.StudentNumber);
        Assert.Equal("Modified Name", updatedPackage.Name);
    }

    [Fact]
    public async Task ReservePackage_HandlesTransactionRollback_WhenErrorOccurs()
    {
        // Arrange
        _context.Packages.Add(_testPackage);
        await _context.SaveChangesAsync();

        // Setup mock to throw exception during reservation
        _mockNoShowService.Setup(s => s.CanStudentReserveAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Simulated error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () =>
            await _reservationService.ReservePackageAsync(_testPackage.Id, _testStudent.IdentityId));

        // Verify package is still available
        var package = await _context.Packages
            .Include(p => p.Reservation)
            .FirstOrDefaultAsync(p => p.Id == _testPackage.Id);
        Assert.Null(package?.Reservation);
    }
}
