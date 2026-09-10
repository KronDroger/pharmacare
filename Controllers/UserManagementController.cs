using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CarePlusPharmacy.Controllers
{
    // RBAC administration — Main Admin assigns/changes staff roles here.
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagementController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);

            ViewBag.UserRoles = userRoles;
            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            TempData["Success"] = $"{user.Email} is now assigned the {newRole} role.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "Action prevented: You cannot deactivate your own active admin account.";
                return RedirectToAction(nameof(Index));
            }

            bool isCurrentlyActive = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow;

            if (isCurrentlyActive)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                await _userManager.UpdateAsync(user);
                TempData["Success"] = $"Account for {user.FullName} ({user.Email}) has been deactivated.";
            }
            else
            {
                user.LockoutEnd = null;
                await _userManager.UpdateAsync(user);
                TempData["Success"] = $"Account for {user.FullName} ({user.Email}) has been activated.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaffUser(string fullName, string email, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Full name, email, and password are required.";
                return RedirectToAction(nameof(Index));
            }

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                TempData["Error"] = $"An account with email '{email}' already exists.";
                return RedirectToAction(nameof(Index));
            }

            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(newUser, password);
            if (!createResult.Succeeded)
            {
                TempData["Error"] = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(role) && await _roleManager.RoleExistsAsync(role))
            {
                await _userManager.AddToRoleAsync(newUser, role);
            }
            else
            {
                await _userManager.AddToRoleAsync(newUser, "Cashier");
            }

            TempData["Success"] = $"New staff member '{fullName}' created successfully with role '{role}'.";
            return RedirectToAction(nameof(Index));
        }
    }
}
