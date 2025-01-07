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
    private readonly UserManager<ApplicationUser> _userManager;

    public PackageManagementController(
        IPackageRepository packageRepository,
        ICafeteriaRepository cafeteriaRepository,
        UserManager<ApplicationUser> userManager)
    {
        _packageRepository = packageRepository;
        _cafeteriaRepository = cafeteriaRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(City? cityFilter, MealType? typeFilter, decimal? maxPrice, bool showExpired = false)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var packages = await _packageRepository.GetAllAsync();
        
        var viewModels = packages
            .Select(p => new PackageManagementViewModel
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
                ReservedBy = p.Reservation?.StudentId
            });

        if (cityFilter.HasValue)
            viewModels = viewModels.Where(p => p.City == cityFilter);

        if (typeFilter.HasValue)
            viewModels = viewModels.Where(p => p.MealType == typeFilter);

        if (maxPrice.HasValue)
            viewModels = viewModels.Where(p => p.Price <= maxPrice);

        if (!showExpired)
            viewModels = viewModels.Where(p => !p.HasExpired);

        var model = new PackageListViewModel
        {
            Packages = viewModels.OrderBy(p => p.PickupDateTime).ToList(),
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

        var cafeteria = await _cafeteriaRepository.GetByLocationAsync(
            Enum.Parse<Domain.Enums.CafeteriaLocation>(user.CafeteriaLocation!));

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

        var cafeteria = await _cafeteriaRepository.GetByLocationAsync(
            Enum.Parse<Domain.Enums.CafeteriaLocation>(user.CafeteriaLocation!));

        if (cafeteria == null)
        {
            ModelState.AddModelError("", "Unable to find your cafeteria location.");
            return View(model);
        }

        if (model.MealType == Domain.Enums.MealType.HotMeal && !cafeteria.OffersHotMeals)
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
            Products = model.Products.Select(name => new Product { Name = name }).ToList()
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
        await _packageRepository.UpdateAsync(package);

        TempData["Success"] = "Package marked as no-show.";
        return RedirectToAction(nameof(Index));
    }
}
