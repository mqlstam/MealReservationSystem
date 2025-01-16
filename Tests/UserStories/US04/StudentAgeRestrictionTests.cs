using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Moq;
using Tests.Helpers;

namespace Tests.UserStories.US04;

public class StudentAgeRestrictionTests
{
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly ApplicationDbContext _context;
    private readonly DateTime _baseDate = new DateTime(2025, 1, 1);

    public StudentAgeRestrictionTests()
    {
        _mockStudentService = new Mock<IStudentService>();
        // We don't need to pass the studentService to Create() anymore
        _context = TestDbContext.Create();
    }

    [Fact]
    public async Task Student_Over18_CanReserveAdultOnlyPackage()
    {
        // Arrange
        var student = CreateStudent(new DateTime(2000, 1, 1)); // 25 years old
        await _context.Students.AddAsync(student);
        await _context.SaveChangesAsync();
        
        var package = await CreateAndSaveAdultOnlyPackage();

        // Act & Assert
        await AssertCanReservePackage(student, package);
    }

    [Fact]
    public async Task Student_Exactly18_CanReserveAdultOnlyPackage()
    {
        // Arrange
        var student = CreateStudent(_baseDate.AddYears(-18)); // Exactly 18
        await _context.Students.AddAsync(student);
        await _context.SaveChangesAsync();
        
        var package = await CreateAndSaveAdultOnlyPackage();

        // Act & Assert
        await AssertCanReservePackage(student, package);
    }

    [Fact]
    public async Task Student_Under18_CannotReserveAdultOnlyPackage()
    {
        // Arrange
        var student = CreateStudent(_baseDate.AddYears(-17)); // 17 years old
        await _context.Students.AddAsync(student);
        await _context.SaveChangesAsync();
        
        var package = await CreateAndSaveAdultOnlyPackage();

        // Act & Assert
        await AssertCannotReservePackage(student, package);
    }

    [Fact]
    public async Task Student_TurningEighteen_OnPickupDay_CanReserveAdultOnlyPackage()
    {
        // Arrange
        var student = CreateStudent(_baseDate.AddDays(5).AddYears(-18)); // Turns 18 on pickup day
        await _context.Students.AddAsync(student);
        await _context.SaveChangesAsync();
        
        var package = await CreateAndSaveAdultOnlyPackage(_baseDate.AddDays(5));

        // Act & Assert
        await AssertCanReservePackage(student, package);
    }

    [Fact]
    public async Task Student_TurningEighteen_AfterPickupDay_CannotReserveAdultOnlyPackage()
    {
        // Arrange
        var student = CreateStudent(_baseDate.AddDays(6).AddYears(-18)); // Turns 18 day after pickup
        await _context.Students.AddAsync(student);
        await _context.SaveChangesAsync();
        
        var package = await CreateAndSaveAdultOnlyPackage(_baseDate.AddDays(5));

        // Act & Assert
        await AssertCannotReservePackage(student, package);
    }

    private Student CreateStudent(DateTime dateOfBirth) => new()
    {
        StudentNumber = "123456",
        FirstName = "Test",
        LastName = "Student",
        Email = "test@test.com",
        DateOfBirth = dateOfBirth,
        StudyCity = City.Breda,
        IdentityId = "test-id"
    };

    private async Task<Package> CreateAndSaveAdultOnlyPackage(DateTime? pickupDate = null)
    {
        var cafeteria = new Cafeteria
        {
            City = City.Breda,
            Location = CafeteriaLocation.LA,
            OffersHotMeals = true
        };
        _context.Cafeterias.Add(cafeteria);
        await _context.SaveChangesAsync();

        var package = new Package
        {
            Name = "Adult Only Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = pickupDate ?? _baseDate.AddDays(1),
            LastReservationDateTime = _baseDate,
            IsAdultOnly = true,
            Price = 5.00m,
            MealType = MealType.Mixed,
            CafeteriaId = cafeteria.Id
        };

        _context.Packages.Add(package);
        await _context.SaveChangesAsync();
        return package;
    }

    private async Task AssertCanReservePackage(Student student, Package package)
    {
        var reservation = new Reservation
        {
            PackageId = package.Id,
            StudentNumber = student.StudentNumber,
            ReservationDateTime = DateTime.Now
        };

        // Act & Assert - should not throw
        await _context.Reservations.AddAsync(reservation);
        await _context.SaveChangesAsync();
    }

    private async Task AssertCannotReservePackage(Student student, Package package)
    {
        var reservation = new Reservation
        {
            PackageId = package.Id,
            StudentNumber = student.StudentNumber,
            ReservationDateTime = DateTime.Now
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _context.Reservations.AddAsync(reservation);
            await _context.SaveChangesAsync();
        });
        
        Assert.Equal("Student must be 18 or older to reserve this package", exception.Message);
    }
}
