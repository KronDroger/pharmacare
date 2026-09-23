using CarePlusPharmacy.Data;
using CarePlusPharmacy.Services;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ISimpleCaptchaService _captcha;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ISimpleCaptchaService captcha)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _captcha = captcha;
        }

        // A fresh question is generated per GET and handed to the view as (question text, signed token).
        // The token round-trips as a hidden field and is verified server-side on POST - the answer
        // is never trusted from the client alone.
        private (string Question, string Token) NewCaptcha() => _captcha.Generate();

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var (q, tok) = NewCaptcha();
            ViewBag.CaptchaQuestion = q;
            ViewBag.CaptchaToken = tok;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null,
            string? captchaToken = null, string? captchaAnswer = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!_captcha.Verify(captchaToken, captchaAnswer))
            {
                ModelState.AddModelError(string.Empty, "Incorrect answer to the verification question. Please try again.");
                var (q, tok) = NewCaptcha();
                ViewBag.CaptchaQuestion = q;
                ViewBag.CaptchaToken = tok;
                return View();
            }

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                var (q4, tok4) = NewCaptcha();
                ViewBag.CaptchaQuestion = q4;
                ViewBag.CaptchaToken = tok4;
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                ModelState.AddModelError(string.Empty,
                    "Too many failed sign-in attempts. This account is locked for 15 minutes. Contact an administrator if you believe this is a mistake.");
                var (q5, tok5) = NewCaptcha();
                ViewBag.CaptchaQuestion = q5;
                ViewBag.CaptchaToken = tok5;
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty,
                    "Too many failed sign-in attempts. This account is locked for 15 minutes.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            var (q6, tok6) = NewCaptcha();
            ViewBag.CaptchaQuestion = q6;
            ViewBag.CaptchaToken = tok6;
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            var (q, tok) = NewCaptcha();
            ViewBag.CaptchaQuestion = q;
            ViewBag.CaptchaToken = tok;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string fullName, string email, string password,
            string phone, string? address, string? city,
            DateTime? dateOfBirth, string? gender,
            string? captchaToken = null, string? captchaAnswer = null)
        {
            if (!_captcha.Verify(captchaToken, captchaAnswer))
            {
                ModelState.AddModelError(string.Empty, "Incorrect answer to the verification question. Please try again.");
                var (q0, tok0) = NewCaptcha();
                ViewBag.CaptchaQuestion = q0;
                ViewBag.CaptchaToken = tok0;
                return View();
            }

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(phone))
            {
                ModelState.AddModelError(string.Empty, "Full name, email, phone, and password are required.");
                var (q1, tok1) = NewCaptcha();
                ViewBag.CaptchaQuestion = q1;
                ViewBag.CaptchaToken = tok1;
                return View();
            }

            email = email.Trim();

            // Does a patient profile already exist for this email address?
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email != null && c.Email == email);

            // The account, the role, and the Customer profile (or link) must be
            // committed together atomically — any failure rolls the whole thing back.
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    await tx.RollbackAsync();
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    var (q3, tok3) = NewCaptcha();
                    ViewBag.CaptchaQuestion = q3;
                    ViewBag.CaptchaToken = tok3;
                    return View();
                }

                await _userManager.AddToRoleAsync(user, "Customer");

                if (existingCustomer != null)
                {
                    // A patient with this email is already on file. Only link the new
                    // account when the phone matches their record — otherwise the account
                    // is not created (staff must link it manually).
                    if (!PhonesMatch(existingCustomer.Phone, phone))
                    {
                        await tx.RollbackAsync();
                        ModelState.AddModelError(string.Empty,
                            "A patient profile already exists for this email. Contact the pharmacy with your valid ID so staff can link your account.");
                        var (q2, tok2) = NewCaptcha();
                        ViewBag.CaptchaQuestion = q2;
                        ViewBag.CaptchaToken = tok2;
                        return View();
                    }

                    user.CustomerId = existingCustomer.Id;
                }
                else
                {
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

                    user.CustomerId = customer.Id;
                }

                await _userManager.UpdateAsync(user);
                await tx.CommitAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Normalizes phone numbers (digits only) so "0918-222-1111" matches "09182221111".
        private static bool PhonesMatch(string? storedPhone, string? enteredPhone)
        {
            static string DigitsOnly(string? value) => string.Concat((value ?? string.Empty).Where(char.IsDigit));
            return !string.IsNullOrWhiteSpace(storedPhone)
                   && !string.IsNullOrWhiteSpace(enteredPhone)
                   && string.Equals(DigitsOnly(storedPhone), DigitsOnly(enteredPhone), StringComparison.Ordinal);
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
