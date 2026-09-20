using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using CarePlusPharmacy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CarePlusPharmacy.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRecaptchaService _recaptchaService;
        private readonly ApplicationDbContext _context;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IRecaptchaService recaptchaService,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _recaptchaService = recaptchaService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            var recaptchaToken = Request.Form["g-recaptcha-response"].ToString();
            var recaptchaResult = await _recaptchaService.VerifyTokenAsync(recaptchaToken, HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!recaptchaResult.Success)
            {
                ModelState.AddModelError(string.Empty, recaptchaResult.ErrorMessage ?? "reCAPTCHA verification failed. Please try again.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "This account has been deactivated. Please contact an administrator.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View();
        }

        [HttpPost]
        public IActionResult ConfirmCaptcha()
        {
            var token = _recaptchaService.GenerateChallengeToken();
            return Json(new { success = true, token });
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string fullName, string email, string password,
            string phone, string? address, string? city,
            DateTime? dateOfBirth, string? gender)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(phone))
            {
                ModelState.AddModelError(string.Empty, "Full name, email, phone, and password are required.");
                return View();
            }

            var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");

                var customer = new Customer
                {
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    Address = address,
                    City = city,
                    DateOfBirth = dateOfBirth,
                    Gender = gender,
                    DateRegistered = DateTime.Today
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
