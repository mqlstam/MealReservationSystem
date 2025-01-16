using System.Security.Claims;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApp.Controllers;
using WebApp.Models.Package;

public class PackageManagementCreateEditTests
{
    private readonly Mock<IPackageRepository> _mockPackageRepository;
    private readonly Mock<ICafeteriaRepository> _mockCafeteriaRepository;
    private readonly Mock<IStudentService> _mockStudentService;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly PackageManagementController _controller;
    private readonly ApplicationUser _testUser;
    private readonly ClaimsPrincipal _userPrincipal;

    public PackageManagementCreateEditTests()
    {
        _mockPackageRepository = new Mock<IPackageRepository>();
        _mockCafeteriaRepository = new Mock<ICafeteriaRepository>();
        _mockStudentService = new Mock<IStudentService>();
        
        var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            mockUserStore.Object, null, null, null, null, null, null, null, null);

        // Setup test user
        _testUser = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "test@test.com",
            CafeteriaLocation = CafeteriaLocation.LA.ToString()
        };

        // Create ClaimsPrincipal for the test user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _testUser.Id),
            new Claim(ClaimTypes.Name, _testUser.UserName),
            new Claim(ClaimTypes.Role, "CafeteriaEmployee")
        };
        _userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Setup UserManager to return test user
        _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        _controller = new PackageManagementController(
            _mockPackageRepository.Object,
            _mockCafeteriaRepository.Object,
            _mockStudentService.Object,
            _mockUserManager.Object);

        // Setup controller context
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = _userPrincipal }
        };
    }

    [Fact]
    public async Task Edit_Get_ReturnsNotFound_WhenPackageDoesNotExist()
    {
        // Arrange
        _mockPackageRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Package)null);

        // Act
        var result = await _controller.Edit(1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
    
    [Fact]
    public async Task Edit_Get_ReturnsView_WhenPackageExists()
    {
        // Arrange
        var package = new Package
        {
            Id = 1,
            Name = "Test Package",
            City = City.Breda,
            CafeteriaLocation = CafeteriaLocation.LA,
            PickupDateTime = DateTime.Now.AddDays(1),
            LastReservationDateTime = DateTime.Now.AddHours(23),
            Price = 5.00m,
            MealType = MealType.BreadAssortment,
            Products = new List<Product>
            {
                new() { Name = "Test Product 1" },
                new() { Name = "Test Product 2" }
            }
        };

        _mockPackageRepository.Setup(repo => repo.GetByIdAsync(1))
            .ReturnsAsync(package);

        // Act
        var result = await _controller.Edit(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CreatePackageViewModel>(viewResult.Model);
        Assert.Equal(package.Name, model.Name);
        Assert.Equal(2, model.ExampleProducts.Count);
        Assert.Equal(1, viewResult.ViewData["PackageId"]);
    }
}