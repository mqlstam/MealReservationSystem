using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.DTOs.Common;
using Application.DTOs.Packages;
using Application.DTOs.Reservation;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

[Authorize(Roles = "Student")]
public class ReservationController : Controller
{
    private readonly IPackageViewService _packageViewService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMappingService _mappingService;

    public ReservationController(
        IPackageViewService packageViewService,
        UserManager<ApplicationUser> userManager,
        IMappingService mappingService)
    {
        _packageViewService = packageViewService;
        _userManager = userManager;
        _mappingService = mappingService;
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
        
        var viewModel = new AvailablePackagesViewModel
        {
            Packages = packages.ToList(), // Now this works because types match
            CityFilter = cityFilter,
            TypeFilter = typeFilter,
            MaxPriceFilter = maxPriceFilter
        };

        return View(viewModel);
    }

    public async Task<IActionResult> MyReservations()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var reservations = await _packageViewService.GetStudentReservationsAsync(user.Id);
        var noShowCount = await _packageViewService.GetStudentNoShowCountAsync(user.Id);

        var viewModel = new MyReservationsViewModel
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

        return View(viewModel);
    }
}
