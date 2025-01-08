using Application.Common.Interfaces;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models.Reservation;

namespace WebApp.Controllers;

[Authorize(Roles = "Student")]
public class ReservationController : Controller
{
    private readonly IPackageRepository _packageRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IStudentService _studentService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReservationController(
        IPackageRepository packageRepository,
        IReservationRepository reservationRepository,
        IStudentService studentService,
        UserManager<ApplicationUser> userManager)
    {
        _packageRepository = packageRepository;
        _reservationRepository = reservationRepository;
        _studentService = studentService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Available(City? cityFilter, MealType? typeFilter, decimal? maxPrice)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var student = await _studentService.GetStudentByIdentityIdAsync(user.Id);
        if (student == null)
        {
            // Create student record if it doesn't exist
            student = await _studentService.GetOrCreateStudentAsync(
                user.Id,
                user.StudentNumber!,
                user.Email!,
                user.FirstName!,
                user.LastName!,
                user.DateOfBirth!.Value,
                Enum.Parse<City>(user.StudyCity!));
        }

        var packages = await _packageRepository.GetAvailablePackagesAsync();
        var noShowCount = student.NoShowCount;
        
        var viewModels = packages.Select(async p =>
        {
            var canReserve = true;
            string? blockReason = null;

            // Check age restriction
            if (p.IsAdultOnly && !student.IsOfLegalAge)
            {
                canReserve = false;
                blockReason = "This package is restricted to users 18 and older.";
            }

            // Check no-show limit
            if (noShowCount >= 2)
            {
                canReserve = false;
                blockReason = "You have reached the maximum number of no-shows.";
            }

            // Check existing reservation for the day
            if (await _reservationRepository.HasReservationForDateAsync(user.Id, p.PickupDateTime.Date))
            {
                canReserve = false;
                blockReason = "You already have a reservation for this date.";
            }

            return new AvailablePackageItem
            {
                Id = p.Id,
                Name = p.Name,
                City = p.City,
                Location = p.CafeteriaLocation,
                PickupDateTime = p.PickupDateTime,
                LastReservationDateTime = p.LastReservationDateTime,
                IsAdultOnly = p.IsAdultOnly,
                Price = p.Price,
                MealType = p.MealType,
                ExampleProducts = p.Products.Select(prod => prod.Name).ToList(),
                CanReserve = canReserve,
                ReservationBlockReason = blockReason
            };
        });

        var items = await Task.WhenAll(viewModels);
        var filteredItems = items.AsQueryable();

        if (cityFilter.HasValue)
            filteredItems = filteredItems.Where(p => p.City == cityFilter);

        if (typeFilter.HasValue)
            filteredItems = filteredItems.Where(p => p.MealType == typeFilter);

        if (maxPrice.HasValue)
            filteredItems = filteredItems.Where(p => p.Price <= maxPrice);

        var model = new AvailablePackagesViewModel
        {
            Packages = filteredItems.OrderBy(p => p.PickupDateTime).ToList(),
            CityFilter = cityFilter,
            TypeFilter = typeFilter,
            MaxPriceFilter = maxPrice
        };

        return View(model);
    }

    public async Task<IActionResult> MyReservations()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var student = await _studentService.GetStudentByIdentityIdAsync(user.Id);
        if (student == null)
            return RedirectToAction(nameof(Available));

        var reservations = await _reservationRepository.GetByStudentIdAsync(user.Id);

        var model = new MyReservationsViewModel
        {
            NoShowCount = student.NoShowCount,
            Reservations = reservations.Select(r => new ReservationItem
            {
                Id = r.Id,
                PackageName = r.Package.Name,
                City = r.Package.City,
                Location = r.Package.CafeteriaLocation,
                PickupDateTime = r.Package.PickupDateTime,
                Price = r.Package.Price,
                MealType = r.Package.MealType,
                Products = r.Package.Products.Select(p => p.Name).ToList(),
                IsPickedUp = r.IsPickedUp,
                IsNoShow = r.IsNoShow
            }).OrderByDescending(r => r.PickupDateTime).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reserve(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var student = await _studentService.GetStudentByIdentityIdAsync(user.Id);
        if (student == null)
        {
            TempData["Error"] = "Student record not found.";
            return RedirectToAction(nameof(Available));
        }

        var package = await _packageRepository.GetByIdAsync(id);
        if (package == null)
        {
            TempData["Error"] = "Package not found.";
            return RedirectToAction(nameof(Available));
        }

        // Check if package is already reserved
        if (package.Reservation != null)
        {
            TempData["Error"] = "This package has already been reserved.";
            return RedirectToAction(nameof(Available));
        }

        // Check age restriction
        if (package.IsAdultOnly && !student.IsOfLegalAge)
        {
            TempData["Error"] = "This package is restricted to users 18 and older.";
            return RedirectToAction(nameof(Available));
        }

        // Check no-show limit
        if (student.NoShowCount >= 2)
        {
            TempData["Error"] = "You have reached the maximum number of no-shows allowed.";
            return RedirectToAction(nameof(Available));
        }

        // Check if user already has a reservation for this date
        if (await _reservationRepository.HasReservationForDateAsync(user.Id, package.PickupDateTime.Date))
        {
            TempData["Error"] = "You already have a reservation for this date.";
            return RedirectToAction(nameof(Available));
        }

        var reservation = new Domain.Entities.Reservation
        {
            PackageId = package.Id,
            StudentNumber = student.StudentNumber,
            ReservationDateTime = DateTime.Now
        };

        await _reservationRepository.AddAsync(reservation);
        TempData["Success"] = "Package reserved successfully!";
        
        return RedirectToAction(nameof(MyReservations));
    }
}
