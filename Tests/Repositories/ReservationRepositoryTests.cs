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
            StudentId = "test-student",
            ReservationDateTime = DateTime.Now
        };
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        // Act
        var hasReservation = await _repository.HasReservationForDateAsync(
            "test-student", 
            DateTime.Now.AddDays(1));

        // Assert
        Assert.True(hasReservation);
    }

    [Fact]
    public async Task GetNoShowCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var cafeteria = new Cafeteria
        {
            City = City.Breda,
            Location = CafeteriaLocation.LA,
            OffersHotMeals = true
        };
        _context.Cafeterias.Add(cafeteria);
        await _context.SaveChangesAsync();

        var package1 = new Package
        {
            Name = "Package 1",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(-1),
            LastReservationDateTime = DateTime.Now.AddDays(-2),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            CafeteriaId = cafeteria.Id
        };
        var package2 = new Package
        {
            Name = "Package 2",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(-2),
            LastReservationDateTime = DateTime.Now.AddDays(-3),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            CafeteriaId = cafeteria.Id
        };
        _context.Packages.AddRange(package1, package2);
        await _context.SaveChangesAsync();

        var reservation1 = new Reservation
        {
            PackageId = package1.Id,
            StudentId = "test-student",
            ReservationDateTime = DateTime.Now.AddDays(-2),
            IsNoShow = true
        };
        var reservation2 = new Reservation
        {
            PackageId = package2.Id,
            StudentId = "test-student",
            ReservationDateTime = DateTime.Now.AddDays(-3),
            IsNoShow = true
        };
        _context.Reservations.AddRange(reservation1, reservation2);
        await _context.SaveChangesAsync();

        // Act
        var noShowCount = await _repository.GetNoShowCountAsync("test-student");

        // Assert
        Assert.Equal(2, noShowCount);
    }
}
