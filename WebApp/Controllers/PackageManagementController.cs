using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Domain.Enums;
using WebApp.Models.Package;
using Application.Services.PackageManagement;
using Application.Services.PackageManagement.DTOs;
using Infrastructure.Identity;
using Application.Common.Interfaces; // Needed for ICafeteriaRepository
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;

namespace WebApp.Controllers
{
    [Authorize(Roles = "CafeteriaEmployee")]
    public class PackageManagementController : Controller
    {
        private readonly IPackageManagementService _packageService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICafeteriaRepository _cafeteriaRepository;

        public PackageManagementController(
            IPackageManagementService packageService,
            UserManager<ApplicationUser> userManager,
            ICafeteriaRepository cafeteriaRepository)
        {
            _packageService = packageService;
            _userManager = userManager;
            _cafeteriaRepository = cafeteriaRepository;
        }

        public async Task<IActionResult> Index(
            bool showOnlyMyCafeteria = false,
            City? cityFilter = null,
            MealType? typeFilter = null,
            decimal? maxPrice = null,
            bool showExpired = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var dto = await _packageService.GetPackageListAsync(
                user.Id,
                showOnlyMyCafeteria,
                cityFilter,
                typeFilter,
                maxPrice,
                showExpired
            );

            var vm = new PackageListViewModel
            {
                CityFilter = dto.CityFilter,
                TypeFilter = dto.TypeFilter,
                MaxPriceFilter = dto.MaxPriceFilter,
                ShowExpired = dto.ShowExpired,
                Packages = dto.Packages.Select(d => new PackageManagementViewModel
                    {
                        Id = d.Id,
                        Name = d.Name,
                        City = d.City,
                        CafeteriaLocation = d.CafeteriaLocation,
                        PickupDateTime = d.PickupDateTime,
                        LastReservationDateTime = d.LastReservationDateTime,
                        IsAdultOnly = d.IsAdultOnly,
                        Price = d.Price,
                        MealType = d.MealType,
                        Products = d.Products,
                        IsReserved = d.IsReserved,
                        IsPickedUp = d.IsPickedUp,
                        IsNoShow = d.IsNoShow,
                        ReservedBy = d.ReservedBy
                    })
                    .OrderBy(p => p.PickupDateTime)
                    .ToList()
            };

            ViewData["ShowOnlyMyCafeteria"] = showOnlyMyCafeteria;
            return View(vm);
        }
        
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Retrieve the logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Parse the cafeteria location from the user
            if (!Enum.TryParse(user.CafeteriaLocation, out CafeteriaLocation locationEnum))
            {
                locationEnum = CafeteriaLocation.LA; // fallback
            }

            // Fetch the cafeteria from the repo so we can display City + Location
            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(locationEnum);
            if (cafeteria == null)
            {
                ViewBag.CityName = "Unknown City";
                ViewBag.CafeteriaLocationName = "Unknown Cafeteria";
            }
            else
            {
                ViewBag.CityName = cafeteria.City.ToString();
                ViewBag.CafeteriaLocationName = cafeteria.Location.ToString();
            }

            // Return default times for the create form
            return View(new CreatePackageViewModel
            {
                PickupDateTime = DateTime.Now.AddHours(1),
                LastReservationDateTime = DateTime.Now.AddMinutes(30)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePackageViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Map from CreatePackageViewModel (web) -> CreatePackageDto (application)
            var dto = new CreatePackageDto
            {
                Name = model.Name,
                PickupDateTime = model.PickupDateTime,
                LastReservationDateTime = model.LastReservationDateTime,
                IsAdultOnly = model.IsAdultOnly,
                Price = model.Price,
                MealType = model.MealType,
                ExampleProducts = model.ExampleProducts
            };

            var (success, errorMsg) = await _packageService.CreatePackageAsync(user.Id, dto);
            if (!success)
            {
                ModelState.AddModelError("", errorMsg);
                return View(model);
            }

            TempData["Success"] = "Package created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var (found, reserved, dto, errorMsg) = await _packageService.GetEditPackageAsync(id);
            if (!found)
                return NotFound();

            if (reserved)
            {
                TempData["Error"] = errorMsg;
                return RedirectToAction(nameof(Index));
            }
            if (dto == null && errorMsg != null)
            {
                TempData["Error"] = errorMsg;
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!Enum.TryParse(user.CafeteriaLocation, out CafeteriaLocation locationEnum))
            {
                locationEnum = CafeteriaLocation.LA;
            }

            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(locationEnum);
            if (cafeteria == null)
            {
                ViewBag.CityName = "Unknown City";
                ViewBag.CafeteriaLocationName = "Unknown Cafeteria";
            }
            else
            {
                ViewBag.CityName = cafeteria.City.ToString();
                ViewBag.CafeteriaLocationName = cafeteria.Location.ToString();
            }

            // Map from CreatePackageDto -> CreatePackageViewModel
            var vm = new CreatePackageViewModel
            {
                Name = dto!.Name,
                PickupDateTime = dto.PickupDateTime,
                LastReservationDateTime = dto.LastReservationDateTime,
                IsAdultOnly = dto.IsAdultOnly,
                Price = dto.Price,
                MealType = dto.MealType,
                ExampleProducts = dto.ExampleProducts
            };

            ViewBag.PackageId = id;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreatePackageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.PackageId = id;
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var dto = new CreatePackageDto
            {
                Name = model.Name,
                PickupDateTime = model.PickupDateTime,
                LastReservationDateTime = model.LastReservationDateTime,
                IsAdultOnly = model.IsAdultOnly,
                Price = model.Price,
                MealType = model.MealType,
                ExampleProducts = model.ExampleProducts
            };

            var (success, err) = await _packageService.UpdatePackageAsync(id, dto, user.Id);
            if (!success)
            {
                // The test wants a redirect if editing fails.
                TempData["Error"] = err;
                // Return a redirect, not a View:
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Package updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var (found, reserved, dto, errorMsg) = await _packageService.GetEditPackageAsync(id);
            if (!found)
                return NotFound();

            var vm = new PackageManagementViewModel
            {
                Id = id,
                Name = dto?.Name ?? "",
                PickupDateTime = dto?.PickupDateTime ?? DateTime.Now,
                LastReservationDateTime = dto?.LastReservationDateTime ?? DateTime.Now,
                IsAdultOnly = dto?.IsAdultOnly ?? false,
                Price = dto?.Price ?? 0,
                MealType = dto?.MealType ?? MealType.BreadAssortment,
                Products = dto?.ExampleProducts ?? new List<string>(),
                IsReserved = reserved
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var (success, errorMsg) = await _packageService.DeletePackageAsync(id);
            if (!success)
            {
                TempData["Error"] = errorMsg;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Package deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPickedUp(int id)
        {
            var (success, msg) = await _packageService.MarkAsPickedUpAsync(id);
            if (!success)
            {
                TempData["Error"] = msg;
            }
            else
            {
                TempData["Success"] = msg;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsNoShow(int id)
        {
            var (success, msg) = await _packageService.MarkAsNoShowAsync(id);
            if (!success)
            {
                TempData["Error"] = msg;
            }
            else
            {
                TempData["Success"] = msg;
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UndoNoShow(int id)
        {
            var (success, msg) = await _packageService.UndoNoShowAsync(id);
            if (!success)
            {
                TempData["Error"] = msg;
            }
            else
            {
                TempData["Success"] = msg;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
