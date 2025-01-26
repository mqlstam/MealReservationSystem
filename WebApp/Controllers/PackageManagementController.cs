using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Domain.Enums;
using Application.Services.PackageManagement;
using Application.Services.PackageManagement.DTOs;
using Infrastructure.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Application.DTOs.PackageManagement;

namespace WebApp.Controllers
{
    [Authorize(Roles = "CafeteriaEmployee")]
    public class PackageManagementController : Controller
    {
        private readonly IPackageManagementService _packageService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PackageManagementController(
            IPackageManagementService packageService,
            UserManager<ApplicationUser> userManager
        )
        {
            _packageService = packageService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            bool showOnlyMyCafeteria = false,
            City? cityFilter = null,
            MealType? typeFilter = null,
            decimal? maxPrice = null,
            bool showExpired = false
        )
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var (found, cafeteriaDto, error) = await _packageService.GetEmployeeCafeteriaAsync(user.Id);
            if (!found)
            {
                ViewBag.CityName = "Unknown City";
                ViewBag.CafeteriaLocationName = "Unknown Cafeteria";
            }
            else
            {
                ViewBag.CityName = cafeteriaDto.CityName;
                ViewBag.CafeteriaLocationName = cafeteriaDto.CafeteriaLocationName;
            }

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

            var dto = new CreatePackageDto
            {
                Name = model.Name,
                PickupDateTime = model.PickupDateTime,
                LastReservationDateTime = model.LastReservationDateTime,
                Price = model.Price,
                MealType = model.MealType,
                ExampleProducts = model.ExampleProducts,
                AlcoholicProducts = model.AlcoholicProducts // ADD THIS LINE!
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
            if (!found) return NotFound();
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

            var (foundCaf, cafeteriaDto, err) = await _packageService.GetEmployeeCafeteriaAsync(user.Id);
            if (!foundCaf)
            {
                ViewBag.CityName = "Unknown City";
                ViewBag.CafeteriaLocationName = "Unknown Cafeteria";
            }
            else
            {
                ViewBag.CityName = cafeteriaDto.CityName;
                ViewBag.CafeteriaLocationName = cafeteriaDto.CafeteriaLocationName;
            }

            var vm = new CreatePackageViewModel
            {
                Name = dto.Name,
                PickupDateTime = dto.PickupDateTime,
                LastReservationDateTime = dto.LastReservationDateTime,
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
                Price = model.Price,
                MealType = model.MealType,
                ExampleProducts = model.ExampleProducts
            };

            var (success, err) = await _packageService.UpdatePackageAsync(id, dto, user.Id);
            if (!success)
            {
                TempData["Error"] = err;
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = "Package updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var (found, reserved, dto, errorMsg) = await _packageService.GetEditPackageAsync(id);
            if (!found) return NotFound();

            var vm = new PackageManagementViewModel
            {
                Id = id,
                Name = dto?.Name ?? "",
                PickupDateTime = dto?.PickupDateTime ?? DateTime.Now,
                LastReservationDateTime = dto?.LastReservationDateTime ?? DateTime.Now,
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



