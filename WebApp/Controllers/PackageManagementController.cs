using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models.Package;

namespace WebApp.Controllers
{
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

        public async Task<IActionResult> Index(
            bool showOnlyMyCafeteria = false,
            City? cityFilter = null,
            MealType? typeFilter = null,
            decimal? maxPrice = null,
            bool showExpired = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Get all packages
            var packages = await _packageRepository.GetAllAsync();
            var packageViewModels = new List<PackageManagementViewModel>();

            foreach (var package in packages)
            {
                var student = (package.Reservation?.StudentNumber != null)
                    ? await _studentService.GetStudentByNumberAsync(package.Reservation.StudentNumber)
                    : null;

                var vm = new PackageManagementViewModel
                {
                    Id = package.Id,
                    Name = package.Name,
                    City = package.City,
                    CafeteriaLocation = package.CafeteriaLocation,
                    PickupDateTime = package.PickupDateTime,
                    LastReservationDateTime = package.LastReservationDateTime,
                    IsAdultOnly = package.IsAdultOnly,
                    Price = package.Price,
                    MealType = package.MealType,
                    Products = package.Products.Select(prod => prod.Name).ToList(),
                    IsReserved = package.Reservation != null,
                    IsPickedUp = package.Reservation?.IsPickedUp ?? false,
                    IsNoShow = package.Reservation?.IsNoShow ?? false,
                    ReservedBy = student != null ? $"{student.FirstName} {student.LastName}" : null
                };

                packageViewModels.Add(vm);
            }

            // Optionally filter by the employee’s own cafeteria
            if (showOnlyMyCafeteria && !string.IsNullOrEmpty(user.CafeteriaLocation))
            {
                // Parse user’s cafeteria location (ignore case)
                if (Enum.TryParse<CafeteriaLocation>(user.CafeteriaLocation, true, out var myLocation))
                {
                    packageViewModels = packageViewModels
                        .Where(p => p.CafeteriaLocation == myLocation)
                        .ToList();
                }
            }

            // Additional filters
            var finalViewModels = packageViewModels.AsQueryable();

            if (cityFilter.HasValue)
                finalViewModels = finalViewModels.Where(p => p.City == cityFilter.Value);

            if (typeFilter.HasValue)
                finalViewModels = finalViewModels.Where(p => p.MealType == typeFilter.Value);

            if (maxPrice.HasValue)
                finalViewModels = finalViewModels.Where(p => p.Price <= maxPrice.Value);

            if (!showExpired)
                finalViewModels = finalViewModels.Where(p => !p.IsExpired);

            var orderedPackages = finalViewModels.OrderBy(p => p.PickupDateTime).ToList();

            var model = new PackageListViewModel
            {
                Packages = orderedPackages,
                CityFilter = cityFilter,
                TypeFilter = typeFilter,
                MaxPriceFilter = maxPrice,
                ShowExpired = showExpired
            };

            ViewData["ShowOnlyMyCafeteria"] = showOnlyMyCafeteria;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // First parse user’s cafeteria location (ignore case)
            if (string.IsNullOrEmpty(user.CafeteriaLocation)
                || !Enum.TryParse<CafeteriaLocation>(user.CafeteriaLocation, true, out var location))
            {
                TempData["Error"] = "Your account is not properly configured for cafeteria management.";
                return RedirectToAction(nameof(Index));
            }

            // Attempt to find the cafeteria
            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(location);
            if (cafeteria == null)
            {
                TempData["Error"] = "Unable to find your cafeteria location.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CreatePackageViewModel
            {
                City = cafeteria.City,
                CafeteriaLocation = cafeteria.Location,
                PickupDateTime = DateTime.Now.AddHours(1),
                LastReservationDateTime = DateTime.Now.AddMinutes(30)
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

            // Parse user’s cafeteria location (ignore case)
            if (string.IsNullOrEmpty(user.CafeteriaLocation)
                || !Enum.TryParse<CafeteriaLocation>(user.CafeteriaLocation, true, out var location))
            {
                ModelState.AddModelError("", "Your account is not properly configured for cafeteria management.");
                return View(model);
            }

            // Attempt to find the cafeteria
            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(location);
            if (cafeteria == null)
            {
                ModelState.AddModelError("", "Unable to find your cafeteria location.");
                return View(model);
            }

            // Clean up blank product lines
            model.ExampleProducts = model.ExampleProducts
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (model.ExampleProducts.Count == 0)
            {
                ModelState.AddModelError("ExampleProducts", "At least one product is required");
                return View(model);
            }

            // Check if hot meals are allowed at cafeteria location
            if (model.MealType == MealType.HotMeal && !cafeteria.OffersHotMeals)
            {
                ModelState.AddModelError("MealType", "Your location does not offer hot meals.");
                return View(model);
            }

            // Must reserve before pickup
            if (model.LastReservationDateTime >= model.PickupDateTime)
            {
                ModelState.AddModelError("LastReservationDateTime",
                    "Last reservation time must be before pickup time.");
                return View(model);
            }

            // Check if user’s cafeteria location matches the package location
            if (model.CafeteriaLocation.ToString() != user.CafeteriaLocation)
            {
                ModelState.AddModelError("", "Package location must match your assigned location.");
                return View(model);
            }

            var newPackage = new Package
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
                Products = model.ExampleProducts
                    .Select(name => new Product { Name = name })
                    .ToList()
            };

            await _packageRepository.AddAsync(newPackage);
            TempData["Success"] = "Package created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null)
            {
                return NotFound();
            }

            // Check if already reserved
            if (package.Reservation != null)
            {
                TempData["Error"] = "Cannot edit a package that is already reserved.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new CreatePackageViewModel
            {
                Name = package.Name,
                City = package.City,
                CafeteriaLocation = package.CafeteriaLocation,
                PickupDateTime = package.PickupDateTime,
                LastReservationDateTime = package.LastReservationDateTime,
                IsAdultOnly = package.IsAdultOnly,
                Price = package.Price,
                MealType = package.MealType,
                ExampleProducts = package.Products?.Select(p => p.Name).ToList() ?? new List<string>()
            };

            ViewBag.PackageId = id;
            return View(viewModel);
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

            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null)
            {
                return NotFound(new { message = "Package not found." });
            }

            if (package.Reservation != null)
            {
                TempData["Error"] = "Cannot edit a package that is already reserved.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Get cafeteria by the package's stored location
            var cafeteria = await _cafeteriaRepository.GetByLocationAsync(package.CafeteriaLocation);
            if (cafeteria == null)
            {
                TempData["Error"] = "Unable to find cafeteria location.";
                return RedirectToAction(nameof(Index));
            }

            model.ExampleProducts = model.ExampleProducts
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (model.ExampleProducts.Count == 0)
            {
                ModelState.AddModelError("ExampleProducts", "At least one product is required");
                ViewBag.PackageId = id;
                return View(model);
            }

            if (model.MealType == MealType.HotMeal && !cafeteria.OffersHotMeals)
            {
                ModelState.AddModelError("MealType", "Your location does not offer hot meals.");
                ViewBag.PackageId = id;
                return View(model);
            }

            if (model.LastReservationDateTime >= model.PickupDateTime)
            {
                ModelState.AddModelError("LastReservationDateTime",
                    "Last reservation time must be before pickup time.");
                ViewBag.PackageId = id;
                return View(model);
            }

            package.Name = model.Name;
            package.City = model.City;
            package.CafeteriaLocation = model.CafeteriaLocation;
            package.PickupDateTime = model.PickupDateTime;
            package.LastReservationDateTime = model.LastReservationDateTime;
            package.IsAdultOnly = model.IsAdultOnly;
            package.Price = model.Price;
            package.MealType = model.MealType;

            // Rebuild product list
            package.Products.Clear();
            foreach (var productName in model.ExampleProducts)
            {
                package.Products.Add(new Product { Name = productName });
            }

            await _packageRepository.UpdateAsync(package);
            TempData["Success"] = "Package updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null)
            {
                return NotFound(new { message = "Package not found." });
            }

            if (package.Reservation != null)
            {
                TempData["Error"] = "Cannot delete a package that is already reserved.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new PackageManagementViewModel
            {
                Id = package.Id,
                Name = package.Name,
                City = package.City,
                CafeteriaLocation = package.CafeteriaLocation,
                PickupDateTime = package.PickupDateTime,
                LastReservationDateTime = package.LastReservationDateTime,
                IsAdultOnly = package.IsAdultOnly,
                Price = package.Price,
                MealType = package.MealType,
                Products = package.Products.Select(p => p.Name).ToList(),
                IsReserved = package.Reservation != null,
                IsPickedUp = package.Reservation?.IsPickedUp ?? false,
                IsNoShow = package.Reservation?.IsNoShow ?? false
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package == null)
            {
                return NotFound(new { message = "Package not found." });
            }

            if (package.Reservation != null)
            {
                TempData["Error"] = "Cannot delete a package that is already reserved.";
                return RedirectToAction(nameof(Index));
            }

            await _packageRepository.DeleteAsync(id);
            TempData["Success"] = "Package deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPickedUp(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package?.Reservation == null)
            {
                return NotFound(new { message = "Package or reservation not found." });
            }

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
            {
                return NotFound(new { message = "Package or reservation not found." });
            }

            if (package.Reservation.IsNoShow)
            {
                TempData["Error"] = "Already marked as no-show.";
                return RedirectToAction(nameof(Index));
            }

            package.Reservation.IsNoShow = true;

            var student = await _studentService.GetStudentByNumberAsync(package.Reservation.StudentNumber);
            if (student != null)
            {
                await _studentService.UpdateNoShowCountAsync(
                    package.Reservation.StudentNumber,
                    student.NoShowCount + 1);
            }

            await _packageRepository.UpdateAsync(package);

            TempData["Success"] = "Package marked as no-show.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UndoNoShow(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id);
            if (package?.Reservation == null)
            {
                return NotFound(new { message = "Package or reservation not found." });
            }

            if (!package.Reservation.IsNoShow)
            {
                TempData["Error"] = "This package is not marked as no-show.";
                return RedirectToAction(nameof(Index));
            }

            package.Reservation.IsNoShow = false;
            package.Reservation.IsPickedUp = true; // Correct addition


            var student = await _studentService.GetStudentByNumberAsync(package.Reservation.StudentNumber);
            if (student != null && student.NoShowCount > 0)
            {
                await _studentService.UpdateNoShowCountAsync(
                    package.Reservation.StudentNumber,
                    student.NoShowCount - 1);
            }

            await _packageRepository.UpdateAsync(package);

            TempData["Success"] = "No-show status undone successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
