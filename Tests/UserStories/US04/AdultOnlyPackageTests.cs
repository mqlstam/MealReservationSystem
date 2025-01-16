using Domain.Entities;
using Domain.Enums;

namespace Tests.UserStories.US04;

public class AdultOnlyPackageTests
{
    [Fact]
    public void Package_WithAlcoholicProduct_IsMarkedAsAdultOnly()
    {
        // Arrange
        var alcoholicProduct = new Product { Name = "Beer", IsAlcoholic = true };
        var package = new Package
        {
            Name = "Test Package",
            Products = new List<Product> { alcoholicProduct }
        };

        // Assert
        Assert.True(package.IsAdultOnly);
    }

    [Fact]
    public void Package_WithoutAlcoholicProduct_IsNotMarkedAsAdultOnly()
    {
        // Arrange
        var nonAlcoholicProduct = new Product { Name = "Soda", IsAlcoholic = false };
        var package = new Package
        {
            Name = "Test Package",
            Products = new List<Product> { nonAlcoholicProduct }
        };

        // Assert
        Assert.False(package.IsAdultOnly);
    }
}
