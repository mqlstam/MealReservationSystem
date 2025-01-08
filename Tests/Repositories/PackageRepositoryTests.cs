using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Tests.Helpers;

namespace Tests.Repositories;

public class PackageRepositoryTests
{
    private readonly ApplicationDbContext _context;
    private readonly PackageRepository _repository;

    public PackageRepositoryTests()
    {
        _context = TestDbContext.Create();
        _repository = new PackageRepository(_context);
    }

    [Fact]
    public async Task GetAvailablePackages_ShouldReturnOnlyNonReservedFuturePackages()
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

        var futurePackage = new Package
        {
            Name = "Future Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            CafeteriaId = cafeteria.Id
        };

        var pastPackage = new Package
        {
            Name = "Past Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(-1),
            LastReservationDateTime = DateTime.Now.AddDays(-2),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            CafeteriaId = cafeteria.Id
        };

        var reservedPackage = new Package
        {
            Name = "Reserved Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            CafeteriaId = cafeteria.Id,
            Reservation = new Reservation
            {
                StudentNumber = student.StudentNumber,
                ReservationDateTime = DateTime.Now
            }
        };

        _context.Packages.AddRange(futurePackage, pastPackage, reservedPackage);
        await _context.SaveChangesAsync();

        // Act
        var availablePackages = await _repository.GetAvailablePackagesAsync();

        // Assert
        Assert.Single(availablePackages);
        Assert.Equal("Future Package", availablePackages.First().Name);
    }

    [Fact]
    public async Task GetByLocation_ShouldReturnOnlyPackagesFromSpecificLocation()
    {
        // Arrange
        var cafeteria1 = new Cafeteria
        {
            City = City.Breda,
            Location = CafeteriaLocation.LA,
            OffersHotMeals = true
        };
        var cafeteria2 = new Cafeteria
        {
            City = City.Breda,
            Location = CafeteriaLocation.LD,
            OffersHotMeals = false
        };
        _context.Cafeterias.AddRange(cafeteria1, cafeteria2);
        await _context.SaveChangesAsync();

        var package1 = new Package
        {
            Name = "LA Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            CafeteriaId = cafeteria1.Id
        };

        var package2 = new Package
        {
            Name = "LD Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LD,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            CafeteriaId = cafeteria2.Id
        };

        _context.Packages.AddRange(package1, package2);
        await _context.SaveChangesAsync();

        // Act
        var laPackages = await _repository.GetByLocationAsync(CafeteriaLocation.LA);

        // Assert
        Assert.Single(laPackages);
        Assert.Equal("LA Package", laPackages.First().Name);
    }
}
