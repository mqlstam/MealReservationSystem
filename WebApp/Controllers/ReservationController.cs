using Application.Common.Interfaces.Services;
using Application.DTOs.Common;
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
    private readonly IPackageViewService _packageViewService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReservationController(
        IPackageViewService packageViewService,
        UserManager<ApplicationUser> userManager)
    {
        _packageViewService = packageViewService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Available([FromQuery] City? cityFilter, [FromQuery] MealType? typeFilter, [FromQuery] decimal? maxPriceFilter)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var filter = new PackageFilterDto
        {
            CityFilter = cityFilter,
            TypeFilter = typeFilter,
            MaxPriceFilter = maxPriceFilter
        };

        var packages = await _packageViewService.GetAvailablePackagesAsync(user.Id, filter);
        
        // Apply max price filter in memory if specified
        if (maxPriceFilter.HasValue)
        {
            packages = packages.Where(p => p.Price <= maxPriceFilter.Value).ToList();
        }

        var model = new AvailablePackagesViewModel
        {
            Packages = packages.Select(p => new AvailablePackageItem
            {
                Id = p.Id,
                Name = p.Name,
                City = p.City,
                Location = p.Location,
                PickupDateTime = p.PickupDateTime,
                LastReservationDateTime = p.LastReservationDateTime,
                IsAdultOnly = p.IsAdultOnly,
                Price = p.Price,
                MealType = p.MealType,
                ExampleProducts = p.ExampleProducts,
                CanReserve = p.CanReserve,
                ReservationBlockReason = p.ReservationBlockReason
            }).ToList(),
            CityFilter = cityFilter,
            TypeFilter = typeFilter,
            MaxPriceFilter = maxPriceFilter // Make sure to pass the filter back to the view
        };

        return View(model);
    }

    public async Task<IActionResult> MyReservations()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var reservations = await _packageViewService.GetStudentReservationsAsync(user.Id);
        var noShowCount = await _packageViewService.GetStudentNoShowCountAsync(user.Id);

        var model = new MyReservationsViewModel
        {
            Reservations = reservations.Select(r => new ReservationItem
            {
                Id = r.Id,
                PackageName = r.PackageName,
                City = r.City,
                Location = r.Location,
                PickupDateTime = r.PickupDateTime,
                Price = r.Price,
                MealType = r.MealType,
                Products = r.Products,
                IsPickedUp = r.IsPickedUp,
                IsNoShow = r.IsNoShow
            }).ToList(),
            NoShowCount = noShowCount
        };

        return View(model);
    }
}
