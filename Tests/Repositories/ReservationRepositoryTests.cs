using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Tests.Helpers;

namespace Tests.Repositories;

public class ReservationRepositoryTests
{
    private readonly ApplicationDbContext _context;
    private readonly ReservationRepository _repository;

    public ReservationRepositoryTests()
    {
        _context = TestDbContext.Create();
        _repository = new ReservationRepository(_context);
    }

    [Fact]
    public async Task HasReservationForDate_ShouldReturnTrue_WhenStudentHasReservation()
    {
        // Arrange
        var student = new Student
        {
            StudentNumber = "123456",
            FirstName = "Test",
            LastName = "Student",
            Email = "test@test.com",
            DateOfBirth = new DateTime(2000, 1, 1),
            StudyCity = City.Breda,
            IdentityId = "test-identity-id"
        };
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

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
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            CafeteriaId = cafeteria.Id
        };
        _context.Packages.Add(package);
        await _context.SaveChangesAsync();

        var reservation = new Reservation
        {
            PackageId = package.Id,
            StudentNumber = student.StudentNumber,
            ReservationDateTime = DateTime.Now
        };
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        // Act
        var hasReservation = await _repository.HasReservationForDateAsync(
            student.IdentityId,
            DateTime.Now.AddDays(1));

        // Assert
        Assert.True(hasReservation);
    }

    [Fact]
    public async Task GetNoShowCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var student = new Student
        {
            StudentNumber = "123456",
            FirstName = "Test",
            LastName = "Student",
            Email = "test@test.com",
            DateOfBirth = new DateTime(2000, 1, 1),
            StudyCity = City.Breda,
            IdentityId = "test-identity-id",
            NoShowCount = 2
        };
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        // Act
        var noShowCount = await _repository.GetNoShowCountAsync(student.IdentityId);

        // Assert
        Assert.Equal(2, noShowCount);
    }
}
