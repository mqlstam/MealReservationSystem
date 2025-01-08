using Application.Common.Interfaces;
using Domain.Entities;
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
    private readonly IStudentService _studentService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PackageController(
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
            return RedirectToAction("Index", "Home");
        }

        var package = await _packageRepository.GetByIdAsync(id);
        if (package == null)
        {
            TempData["Error"] = "Package not found.";
            return RedirectToAction("Index", "Home");
        }

        if (package.Reservation != null)
        {
            TempData["Error"] = "Package already reserved.";
            return RedirectToAction("Index", "Home");
        }

        // Check age restriction
        if (package.IsAdultOnly && !student.IsOfLegalAge)
        {
            TempData["Error"] = "You must be 18 or older to reserve this package.";
            return RedirectToAction("Index", "Home");
        }

        // Check no-show count
        if (student.NoShowCount >= 2)
        {
            TempData["Error"] = "You have too many no-shows to make a reservation.";
            return RedirectToAction("Index", "Home");
        }

        // Create reservation
        var reservation = new Reservation
        {
            PackageId = package.Id,
            StudentNumber = student.StudentNumber,
            ReservationDateTime = DateTime.Now
        };

        await _reservationRepository.AddAsync(reservation);
        TempData["Success"] = "Package reserved successfully!";

        return RedirectToAction("Index", "Home");
    }
}
