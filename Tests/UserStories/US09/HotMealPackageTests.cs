// using Domain.Entities;
// using Domain.Enums;
// using Infrastructure.Identity;
// using Infrastructure.Persistence;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using Moq;
// using Tests.Helpers;
// using WebApp.Controllers;
// using WebApp.Models.Package;
// using System.Security.Claims;
//
// namespace Tests.UserStories.US09;
//
// public class HotMealPackageTests
// {
//     private readonly ApplicationDbContext _context;
//     private readonly Mock<IPackageRepository> _mockPackageRepo;
//     private readonly Mock<ICafeteriaRepository> _mockCafeteriaRepo;
//     private readonly Mock<IStudentService> _mockStudentService;
//     private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
//     private readonly PackageManagementController _controller;
//
//     public HotMealPackageTests()
//     {
//         _context = TestDbContext.Create();
//         _mockPackageRepo = new Mock<IPackageRepository>();
//         _mockCafeteriaRepo = new Mock<ICafeteriaRepository>();
//         _mockStudentService = new Mock<IStudentService>();
//         
//         // Setup UserManager mock with proper store
//         var mockStore = new Mock<IUserStore<ApplicationUser>>();
//         _mockUserManager = new Mock<UserManager<ApplicationUser>>(
//             mockStore.Object,
//             null, // IOptions<IdentityOptions>
//             null, // IPasswordHasher<TUser>
//             null, // IEnumerable<IUserValidator<TUser>>
//             null, // IEnumerable<IPasswordValidator<TUser>>
//             null, // ILookupNormalizer
//             null, // IdentityErrorDescriber
//             null, // IServiceProvider
//             null  // ILogger<UserManager<TUser>>
//         );
//
//         _controller = new PackageManagementController(
//             _mockPackageRepo.Object,
//             _mockCafeteriaRepo.Object,
//             _mockStudentService.Object,
//             _mockUserManager.Object);
//
//         // Setup controller context with user
//         var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
//         {
//             new Claim(ClaimTypes.NameIdentifier, "test-id"),
//             new Claim(ClaimTypes.Role, "CafeteriaEmployee")
//         }));
//
//         _controller.ControllerContext = new ControllerContext
//         {
//             HttpContext = new DefaultHttpContext { User = user }
//         };
//
//         // Setup default package repository behavior
//         _mockPackageRepo.Setup(repo => repo.AddAsync(It.IsAny<Package>()))
//             .ReturnsAsync((Package package) => package);
//     }
//
//     [Fact]
//     public async Task Create_SucceedsForHotMeal_WhenLocationOffersHotMeals()
//     {
//         // Arrange
//         var employee = new ApplicationUser
//         {
//             Id = "test-id",
//             CafeteriaLocation = CafeteriaLocation.LA.ToString(),
//             UserName = "employee@test.com"
//         };
//
//         var cafeteria = new Cafeteria
//         {
//             Id = 1,
//             City = City.Breda,
//             Location = CafeteriaLocation.LA,
//             OffersHotMeals = true
//         };
//
//         _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
//             .ReturnsAsync(employee);
//         _mockCafeteriaRepo.Setup(x => x.GetByLocationAsync(CafeteriaLocation.LA))
//             .ReturnsAsync(cafeteria);
//
//         var model = new CreatePackageViewModel
//         {
//             Name = "Hot Meal Package",
//             City = City.Breda,
//             CafeteriaLocation = CafeteriaLocation.LA,
//             PickupDateTime = DateTime.Now.AddHours(2),
//             LastReservationDateTime = DateTime.Now.AddHours(1),
//             Price = 5.00m,
//             MealType = MealType.HotMeal,
//             ExampleProducts = new List<string> { "Rice", "Chicken" }
//         };
//
//         // Act
//         var result = await _controller.Create(model);
//
//         // Assert
//         var redirectResult = Assert.IsType<RedirectToActionResult>(result);
//         Assert.Equal("Index", redirectResult.ActionName);
//         _mockPackageRepo.Verify(x => x.AddAsync(It.IsAny<Package>()), Times.Once);
//     }
//
//     [Fact]
//     public async Task Create_FailsForHotMeal_WhenLocationDoesNotOfferHotMeals()
//     {
//         // Arrange
//         var employee = new ApplicationUser
//         {
//             Id = "test-id",
//             CafeteriaLocation = CafeteriaLocation.HA.ToString(),
//             UserName = "employee@test.com"
//         };
//
//         var cafeteria = new Cafeteria
//         {
//             Id = 1,
//             City = City.Breda,
//             Location = CafeteriaLocation.HA,
//             OffersHotMeals = false
//         };
//
//         _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
//             .ReturnsAsync(employee);
//         _mockCafeteriaRepo.Setup(x => x.GetByLocationAsync(CafeteriaLocation.HA))
//             .ReturnsAsync(cafeteria);
//
//         var model = new CreatePackageViewModel
//         {
//             Name = "Hot Meal Package",
//             City = City.Breda,
//             CafeteriaLocation = CafeteriaLocation.HA,
//             PickupDateTime = DateTime.Now.AddHours(2),
//             LastReservationDateTime = DateTime.Now.AddHours(1),
//             Price = 5.00m,
//             MealType = MealType.HotMeal,
//             ExampleProducts = new List<string> { "Rice", "Chicken" }
//         };
//
//         // Act
//         var result = await _controller.Create(model);
//
//         // Assert
//         var viewResult = Assert.IsType<ViewResult>(result);
//         var returnedModel = Assert.IsType<CreatePackageViewModel>(viewResult.Model);
//         Assert.Contains("Your location does not offer hot meals.", 
//             _controller.ModelState["MealType"].Errors.Select(e => e.ErrorMessage));
//     }
//
//     [Fact]
//     public async Task Create_Succeeds_WhenLocationDoesNotOfferHotMealsButPackageIsNotHotMeal()
//     {
//         // Arrange
//         var employee = new ApplicationUser
//         {
//             Id = "test-id",
//             CafeteriaLocation = CafeteriaLocation.HA.ToString(),
//             UserName = "employee@test.com"
//         };
//
//         var cafeteria = new Cafeteria
//         {
//             Id = 1,
//             City = City.Breda,
//             Location = CafeteriaLocation.HA,
//             OffersHotMeals = false
//         };
//
//         _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
//             .ReturnsAsync(employee);
//         _mockCafeteriaRepo.Setup(x => x.GetByLocationAsync(CafeteriaLocation.HA))
//             .ReturnsAsync(cafeteria);
//
//         var model = new CreatePackageViewModel
//         {
//             Name = "Lunch Package",
//             City = City.Breda,
//             CafeteriaLocation = CafeteriaLocation.HA,
//             PickupDateTime = DateTime.Now.AddHours(2),
//             LastReservationDateTime = DateTime.Now.AddHours(1),
//             Price = 5.00m,
//             MealType = MealType.BreadAssortment,
//             ExampleProducts = new List<string> { "Sandwich", "Fruit" }
//         };
//
//         // Act
//          var result = await _controller.Create(model);
//
//         // Assert
//         var redirectResult = Assert.IsType<RedirectToActionResult>(result);
//         Assert.Equal("Index", redirectResult.ActionName);
//         _mockPackageRepo.Verify(x => x.AddAsync(It.IsAny<Package>()), Times.Once);
//     }
//
//     [Fact]
//     public async Task Create_Fails_WhenCafeteriaNotFound()
//     {
//         // Arrange
//         var employee = new ApplicationUser
//         {
//             Id = "test-id",
//             CafeteriaLocation = CafeteriaLocation.LA.ToString(),
//             UserName = "employee@test.com"
//         };
//
//         _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
//             .ReturnsAsync(employee);
//         _mockCafeteriaRepo.Setup(x => x.GetByLocationAsync(CafeteriaLocation.LA))
//             .ReturnsAsync((Cafeteria)null);
//
//         var model = new CreatePackageViewModel
//         {
//             Name = "Hot Meal Package",
//             City = City.Breda,
//             CafeteriaLocation = CafeteriaLocation.LA,
//             PickupDateTime = DateTime.Now.AddHours(2),
//             LastReservationDateTime = DateTime.Now.AddHours(1),
//             Price = 5.00m,
//             MealType = MealType.HotMeal,
//             ExampleProducts = new List<string> { "Rice", "Chicken" }
//         };
//
//         // Act
//         var result = await _controller.Create(model);
//
//         // Assert
//         var viewResult = Assert.IsType<ViewResult>(result);
//         Assert.Contains("Unable to find your cafeteria location.", 
//             _controller.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
//     }
//
//     [Fact]
//     public async Task Create_Fails_WhenEmployeeLocationMismatchesPackageLocation()
//     {
//          // Arrange
//         var employee = new ApplicationUser
//         {
//             Id = "test-id",
//             CafeteriaLocation = CafeteriaLocation.LA.ToString(),
//             UserName = "employee@test.com"
//         };
//
//         var cafeteria = new Cafeteria
//         {
//             Id = 1,
//             City = City.Breda,
//             Location = CafeteriaLocation.LA,
//             OffersHotMeals = true
//         };
//
//         _mockUserManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
//             .ReturnsAsync(employee);
//         _mockCafeteriaRepo.Setup(x => x.GetByLocationAsync(CafeteriaLocation.LA))
//             .ReturnsAsync(cafeteria);
//
//          var model = new CreatePackageViewModel
//         {
//             Name = "Hot Meal Package",
//             City = City.Breda,
//             CafeteriaLocation = CafeteriaLocation.LD, // Different location than employee
//             PickupDateTime = DateTime.Now.AddHours(2),
//             LastReservationDateTime = DateTime.Now.AddHours(1),
//             Price = 5.00m,
//             MealType = MealType.HotMeal,
//              ExampleProducts = new List<string> { "Rice", "Chicken" }
//          };
//
//         // Act
//         var result = await _controller.Create(model);
//
//         // Assert
//         var viewResult = Assert.IsType<ViewResult>(result);
//         Assert.Contains("Package location must match your assigned location.", 
//             _controller.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
//     }
// }
