using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models.Package;

namespace WebApp.Controllers;

[Authorize]
public class PackageController : Controller
{
    private readonly IPackageRepository _packageRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ICafeteriaRepository _cafeteriaRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public PackageController(
        IPackageRepository packageRepository,
        IReservationRepository reservationRepository,
        ICafeteriaRepository cafeteriaRepository,
        UserManager<ApplicationUser> userManager)
    {
        _packageRepository = packageRepository;
        _reservationRepository = reservationRepository;
        _cafeteriaRepository = cafeteriaRepository;
        _userManager = userManager;
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Available()
    {
        var packages = await _packageRepository.GetAvailablePackagesAsync();
        var currentUser = await _userManager.GetUserAsync(User);
        var userAge = DateTime.Today.Year - currentUser!.DateOfBirth!.Value.Year;

        var viewModels = packages.Select(p => new PackageViewModel
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
            ExampleProducts = p.Products.Select(prod => prod.Name).ToList()
        }).Where(p => !p.IsAdultOnly || userAge >= 18).ToList();

        return View(viewModels);
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyReservations()
    {
        var user = await _userManager.GetUserAsync(User);
        var reservations = await _reservationRepository.GetByStudentIdAsync(user!.Id);

        var viewModels = reservations.Select(r => new PackageViewModel
        {
            Id = r.Package.Id,
            Name = r.Package.Name,
            City = r.Package.City,
            CafeteriaLocation = r.Package.CafeteriaLocation,
            PickupDateTime = r.Package.PickupDateTime,
            LastReservationDateTime = r.Package.LastReservationDateTime,
            IsAdultOnly = r.Package.IsAdultOnly,
            Price = r.Package.Price,
            MealType = r.Package.MealType,
            ExampleProducts = r.Package.Products.Select(p => p.Name).ToList(),
            IsReserved = true,
            IsPickedUp = r.IsPickedUp
        }).ToList();

        return View(viewModels);
    }

    [Authorize(Roles = "CafeteriaEmployee")]
    public async Task<IActionResult> Manage()
    {
        var user = await _userManager.GetUserAsync(User);
        var cafeteria = await _cafeteriaRepository.GetByLocationAsync(
            Enum.Parse<CafeteriaLocation>(user!.CafeteriaLocation!));
        
        var packages = await _packageRepository.GetByLocationAsync(cafeteria!.Location);
        
        var viewModels = packages.Select(p => new PackageViewModel
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
            ExampleProducts = p.Products.Select(prod => prod.Name).ToList(),
            IsReserved = p.Reservation != null
        }).ToList();

        return View(viewModels);
    }

    [Authorize(Roles = "CafeteriaEmployee")]
    public IActionResult Create()
    {
        return View(new CreatePackageViewModel
        {
            PickupDateTime = DateTime.Now.AddHours(1),
            LastReservationDateTime = DateTime.Now.AddMinutes(30)
        });
    }

    [HttpPost]
    [Authorize(Roles = "CafeteriaEmployee")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePackageViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        var cafeteria = await _cafeteriaRepository.GetByLocationAsync(
            Enum.Parse<CafeteriaLocation>(user!.CafeteriaLocation!));

        if (model.MealType == MealType.HotMeal && !cafeteria!.OffersHotMeals)
        {
            ModelState.AddModelError(string.Empty, "Your location does not offer hot meals.");
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
            CafeteriaId = cafeteria!.Id
        };

        await _packageRepository.AddAsync(package);
        return RedirectToAction(nameof(Manage));
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(int id)
    {
        var package = await _packageRepository.GetByIdAsync(id);
        if (package == null)
            return NotFound();

        var user = await _userManager.GetUserAsync(User);
        
        // Check if user has reached the no-show limit
        var noShowCount = await _reservationRepository.GetNoShowCountAsync(user!.Id);
        if (noShowCount >= 2)
        {
            TempData["Error"] = "You cannot make reservations due to multiple no-shows.";
            return RedirectToAction(nameof(Available));
        }

        // Check if user already has a reservation for this date
        if (await _reservationRepository.HasReservationForDateAsync(user!.Id, package.PickupDateTime.Date))
        {
            TempData["Error"] = "You already have a reservation for this date.";
            return RedirectToAction(nameof(Available));
        }

        // Check age restriction
        var userAge = DateTime.Today.Year - user!.DateOfBirth!.Value.Year;
        if (package.IsAdultOnly && userAge < 18)
        {
            TempData["Error"] = "This package is restricted to users 18 and older.";
            return RedirectToAction(nameof(Available));
        }

        var reservation = new Reservation
        {
            PackageId = package.Id,
            StudentId = user.Id,
            ReservationDateTime = DateTime.Now
        };

        await _reservationRepository.AddAsync(reservation);
        return RedirectToAction(nameof(MyReservations));
    }

    [HttpPost]
    [Authorize(Roles = "CafeteriaEmployee")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNoShow(int id)
    {
        var package = await _packageRepository.GetByIdAsync(id);
        if (package?.Reservation == null)
            return NotFound();

        package.Reservation.IsNoShow = true;
        await _reservationRepository.UpdateAsync(package.Reservation);

        return RedirectToAction(nameof(Manage));
    }
}
