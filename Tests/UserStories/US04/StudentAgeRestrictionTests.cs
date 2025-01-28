using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Tests.Helpers;

namespace Tests.UserStories.US04;

public class StudentAgeRestrictionTests
{
    private readonly ApplicationDbContext _context;
    private readonly IAgeVerificationService _ageVerificationService;
    private readonly DateTime _baseDate = new DateTime(2025, 1, 1);

    public StudentAgeRestrictionTests()
    {
        _ageVerificationService = new AgeVerificationService();
        _context = TestDbContext.Create();
    }

    [Fact]
    public void Student_Under18_CannotReserveAdultOnlyPackage()
    {
        // Arrange
        var student = CreateStudent(_baseDate.AddYears(-17)); // 17 years old
        var package = CreateTestPackage(includeAlcohol: true);
        package.UpdateIsAdultOnly(); // Make sure IsAdultOnly is updated

        // Act & Assert
        Assert.False(_ageVerificationService.IsStudentEligibleForPackage(student, package));
    }

    [Fact]
    public void Student_TurningEighteen_AfterPickupDay_CannotReserveAdultOnlyPackage()
    {
        // Arrange
        var pickupDate = _baseDate.AddDays(5);
        var student = CreateStudent(pickupDate.AddDays(1).AddYears(-18)); // Turns 18 day after pickup
        var package = CreateTestPackage(pickupDate: pickupDate, includeAlcohol: true);
        package.UpdateIsAdultOnly(); // Make sure IsAdultOnly is updated

        // Act & Assert
        Assert.False(_ageVerificationService.IsStudentEligibleForPackage(student, package));
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

    private Package CreateTestPackage(DateTime? pickupDate = null, bool includeAlcohol = false)
    {
        var package = new Package
        {
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = pickupDate ?? _baseDate.AddDays(1),
            LastReservationDateTime = _baseDate,
            Price = 5.00m,
            MealType = MealType.Mixed,
            Products = new List<Product>()
        };

        if (includeAlcohol)
        {
            package.Products.Add(new Product { Name = "Beer", IsAlcoholic = true });
        }

        package.UpdateIsAdultOnly();
        return package;
    }
}
