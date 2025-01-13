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
        var studentId = user.Id;
        var isOfLegalAge = student.IsOfLegalAge;
        var studentItems = new List<AvailablePackageItem>();

        foreach (var package in packages)
        {
            var canReserve = true;
            string? blockReason = null;

            if (package.IsAdultOnly && !isOfLegalAge)
            {
                canReserve = false;
                blockReason = "This package is restricted to users 18 and older.";
            }

            if (noShowCount >= 2)
            {
                canReserve = false;
                blockReason = "You have reached the maximum number of no-shows.";
            }

            if (await _reservationRepository.HasReservationForDateAsync(studentId, package.PickupDateTime.Date))
            {
                canReserve = false;
                blockReason = "You already have a reservation for this date.";
            }

            var item = new AvailablePackageItem
            {
                Id = package.Id,
                Name = package.Name,
                City = package.City,
                Location = package.CafeteriaLocation,
                PickupDateTime = package.PickupDateTime,
                LastReservationDateTime = package.LastReservationDateTime,
                IsAdultOnly = package.IsAdultOnly,
                Price = package.Price,
                MealType = package.MealType,
                ExampleProducts = package.Products.Select(prod => prod.Name).ToList(),
                CanReserve = canReserve,
                ReservationBlockReason = blockReason
            };

            studentItems.Add(item);
        }

        var filteredItems = studentItems.AsQueryable();

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

        if (package.Reservation != null)
        {
            TempData["Error"] = "This package has already been reserved.";
            return RedirectToAction(nameof(Available));
        }

        if (package.IsAdultOnly && !student.IsOfLegalAge)
        {
            TempData["Error"] = "This package is restricted to users 18 and older.";
            return RedirectToAction(nameof(Available));
        }

        if (student.NoShowCount >= 2)
        {
            TempData["Error"] = "You have reached the maximum number of no-shows allowed.";
            return RedirectToAction(nameof(Available));
        }

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
