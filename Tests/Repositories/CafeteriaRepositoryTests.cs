using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Tests.Helpers;

namespace Tests.Repositories;

public class CafeteriaRepositoryTests
{
    private readonly ApplicationDbContext _context;
    private readonly CafeteriaRepository _repository;

    public CafeteriaRepositoryTests()
    {
        _context = TestDbContext.Create();
        _repository = new CafeteriaRepository(_context);
    }

    [Fact]
    public async Task OffersHotMeals_ShouldReturnCorrectValue()
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

        // Act
        var offersHotMeals = await _repository.OffersHotMealsAsync(CafeteriaLocation.LA);

        // Assert
        Assert.True(offersHotMeals);
    }

    [Fact]
    public async Task GetByLocation_ShouldReturnCorrectCafeteria()
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
            City = City.DenBosch,
            Location = CafeteriaLocation.DB,
            OffersHotMeals = false
        };
        _context.Cafeterias.AddRange(cafeteria1, cafeteria2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByLocationAsync(CafeteriaLocation.LA);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CafeteriaLocation.LA, result.Location);
        Assert.True(result.OffersHotMeals);
    }
}
