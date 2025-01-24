using Application.Common.Interfaces;
using Application.DTOs.Account;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IStudentService _studentService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IStudentService studentService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _studentService = studentService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DateOfBirth = model.DateOfBirth,
                    StudentNumber = model.IsStudent ? model.StudentNumber : null,
                    EmployeeNumber = !model.IsStudent ? model.EmployeeNumber : null,
                    StudyCity = model.IsStudent ? model.StudyCity : null,
                    CafeteriaLocation = !model.IsStudent ? model.CafeteriaLocation : null
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.IsStudent ? "Student" : "CafeteriaEmployee");

                    if (model.IsStudent && !string.IsNullOrWhiteSpace(model.StudentNumber))
                    {
                        City parsedCity = City.Breda;
                        if (!string.IsNullOrEmpty(model.StudyCity) &&
                            Enum.TryParse(model.StudyCity, out City cityValue))
                        {
                            parsedCity = cityValue;
                        }

                        await _studentService.GetOrCreateStudentAsync(
                            identityId: user.Id,
                            studentNumber: model.StudentNumber,
                            email: user.Email,
                            firstName: user.FirstName,
                            lastName: user.LastName,
                            dateOfBirth: user.DateOfBirth ?? DateTime.UtcNow.AddYears(-20),
                            studyCity: parsedCity,
                            phoneNumber: model.PhoneNumber
                        );
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        var isEmployee = await _userManager.IsInRoleAsync(user, "CafeteriaEmployee");
                        if (isEmployee)
                        {
                            return RedirectToAction("Index", "PackageManagement");
                        }
                    }
            
                    return RedirectToLocal(returnUrl);
                }

                if (result.IsLockedOut)
                {
                    return View("Lockout");
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            
            return RedirectToAction("Index", "Home");
        }
    }
}
