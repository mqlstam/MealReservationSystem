using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models.Package;

namespace WebApp.Controllers
{
    [Authorize]
    public class PackageController : Controller
    {
        private readonly IReservationService _reservationService;
        private readonly UserManager<ApplicationUser> _userManager;
    
        public PackageController(
            IReservationService reservationService,
            UserManager<ApplicationUser> userManager)
        {
            _reservationService = reservationService;
             _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
    
            var result = await _reservationService.ReservePackageAsync(id, user.Id);
    
            if (result == "Package reserved successfully!")
            {
                TempData["Success"] = result;
                return RedirectToAction("Index", "Home");
            }
    
             TempData["Error"] = result;
             return RedirectToAction("Index", "Home");
        }
    }
}
