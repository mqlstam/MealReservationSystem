using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models.Package;

namespace WebApp.Controllers;

[Authorize(Roles = "CafeteriaEmployee")]
public class PackageManagementController : Controller
{
    private readonly IPackageRepository _packageRepository;
    private readonly ICafeteriaRepository _cafeteriaRepository;
    private readonly IStudentService _studentService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PackageManagementController(
        IPackageRepository packageRepository,
        ICafeteriaRepository cafeteriaRepository,
        IStudentService studentService,
        UserManager<ApplicationUser> userManager)
    {
        _packageRepository = packageRepository;
        _cafeteriaRepository = cafeteriaRepository;
        _studentService = studentService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(City? cityFilter, MealType? typeFilter, decimal? maxPrice, bool showExpired = false)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var packages = await _packageRepository.GetAllAsync();
        
        var viewModels = packages.Select(async p =>
        {
            var student = p.Reservation?.StudentNumber != null 
                ? await _studentService.GetStudentByNumberAsync(p.Reservation.StudentNumber)
                : null;

            return new PackageManagementViewModel
            {
                Id = p.Id,
                Name = p.Name,
                City = p.City,
                CafeteriaLocation = p.CafeteriaLocation,
                PickupDateTime = p.PickupDateTime,
                LastReservationDateTime = p.LastReservationDateTime,
                IsAdultOnly = p.IsAdultOnly,
                Price = p.Price,
                MealType = p.MealType,
                Products = p.Products.Select(prod => prod.Name).ToList(),
                IsReserved = p.Reservation != null,
                IsPickedUp = p.Reservation?.IsPickedUp ?? false,
                ReservedBy = student != null ? $"{student.FirstName} {student.LastName}" : null
            };
        });

        var items = await Task.WhenAll(viewModels);
        var finalViewModels = items.AsQueryable();

        if (cityFilter.HasValue)
            finalViewModels = finalViewModels.Where(p => p.City == cityFilter);

        if (typeFilter.HasValue)
            finalViewModels = finalViewModels.Where(p => p.MealType == typeFilter);

        if (maxPrice.HasValue)
            finalViewModels = finalViewModels.Where(p => p.Price <= maxPrice);

        if (!showExpired)
            finalViewModels = finalViewModels.Where(p => !p.HasExpired);

        var model = new PackageListViewModel
        {
            Packages = finalViewModels.OrderBy(p => p.PickupDateTime).ToList(),
            CityFilter = cityFilter,
            TypeFilter = typeFilter,
            MaxPriceFilter = maxPrice,
            ShowExpired = showExpired
        };

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (string.IsNullOrEmpty(user.CafeteriaLocation) || !Enum.TryParse<CafeteriaLocation>(user.CafeteriaLocation, out var location))
        {
            TempData["Error"] = "Your account is not properly configured for cafeteria management.";
            return RedirectToAction("Index", "Home");
        }

        var cafeteria = await _cafeteriaRepository.GetByLocationAsync(location);

        if (cafeteria == null)
        {
            TempData["Error"] = "Unable to find your cafeteria location.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = new CreatePackageViewModel
        {
            City = cafeteria.City,
            CafeteriaLocation = cafeteria.Location
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePackageViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (string.IsNullOrEmpty(user.CafeteriaLocation) || !Enum.TryParse<CafeteriaLocation>(user.CafeteriaLocation, out var location))
        {
            ModelState.AddModelError("", "Your account is not properly configured for cafeteria management.");
            return View(model);
        }

        var cafeteria = await _cafeteriaRepository.GetByLocationAsync(location);

        if (cafeteria == null)
        {
            ModelState.AddModelError("", "Unable to find your cafeteria location.");
            return View(model);
        }

        if (model.MealType == MealType.HotMeal && !cafeteria.OffersHotMeals)
        {
            ModelState.AddModelError("MealType", "Your location does not offer hot meals.");
            return View(model);
        }

        if (model.LastReservationDateTime >= model.PickupDateTime)
        {
            ModelState.AddModelError("LastReservationDateTime", 
                "Last reservation time must be before pickup time.");
            return View(model);
        }

        var package = new Package
        {
            Name = model.Name,
            City = model.City,
            CafeteriaLocation = model.CafeteriaLocation,
            PickupDateTime = model.PickupDateTime,
            LastReservationDateTime = model.LastReservationDateTime,
            IsAdultOnly = model.IsAdultOnly,
            Price = model.Price,
            MealType = model.MealType,
            CafeteriaId = cafeteria.Id,
            Products = model.ExampleProducts.Select(name => new Product { Name = name }).ToList()
        };

        await _packageRepository.AddAsync(package);
        TempData["Success"] = "Package created successfully.";
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsPickedUp(int id)
    {
        var package = await _packageRepository.GetByIdAsync(id);
        if (package?.Reservation == null)
            return NotFound();

        package.Reservation.IsPickedUp = true;
        await _packageRepository.UpdateAsync(package);

        TempData["Success"] = "Package marked as picked up.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsNoShow(int id)
    {
        var package = await _packageRepository.GetByIdAsync(id);
        if (package?.Reservation == null)
            return NotFound();

        package.Reservation.IsNoShow = true;
        
        // Update the student's no-show count
        await _studentService.UpdateNoShowCountAsync(
            package.Reservation.StudentNumber, 
            (await _studentService.GetStudentByNumberAsync(package.Reservation.StudentNumber))?.NoShowCount + 1 ?? 1);
            
        await _packageRepository.UpdateAsync(package);

        TempData["Success"] = "Package marked as no-show.";
        return RedirectToAction(nameof(Index));
    }
}
