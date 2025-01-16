using Application.Common.Interfaces;
using Domain.Enums;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models.Account;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        // 1) Inject IStudentService
        private readonly IStudentService _studentService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IStudentService studentService)  // <-- Add this parameter
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _studentService = studentService; // <-- Store for later use
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
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Create the Identity user record
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
                    // Assign to the correct role
                    await _userManager.AddToRoleAsync(user, model.IsStudent ? "Student" : "CafeteriaEmployee");

                    // 2) If this is a student, also create a Student entry in main DB
                    if (model.IsStudent && !string.IsNullOrWhiteSpace(model.StudentNumber))
                    {
                        // Convert the StudyCity string to our City enum (default to Breda if parse fails)
                        City parsedCity = City.Breda;
                        if (!string.IsNullOrEmpty(model.StudyCity) &&
                            Enum.TryParse(model.StudyCity, out City cityValue))
                        {
                            parsedCity = cityValue;
                        }

                        // Call the StudentService to ensure a Student row exists in the main DB
                        await _studentService.GetOrCreateStudentAsync(
                            identityId: user.Id,
                            studentNumber: model.StudentNumber,
                            email: user.Email,
                            firstName: user.FirstName,
                            lastName: user.LastName,
                            dateOfBirth: user.DateOfBirth ?? DateTime.UtcNow.AddYears(-20),
                            studyCity: parsedCity
                        );
                    }

                    // Sign in and redirect
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                // If result failed, show errors
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Return model with errors
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
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
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
                    // Get the user to check their role
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
                return View(model);
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
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
