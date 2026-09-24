using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Membership Tiers management module — Admin only. These are the paid
    // customer tiers (discount %, free delivery, premium perks) sold on the
    // portal. Distinct from per-medicine SubscriptionPlans.
    [Authorize(Roles = "Admin")]
    public class MembershipTiersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public MembershipTiersController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var tiers = await _context.MembershipTiers
                .Include(t => t.Memberships)
                .OrderBy(t => t.MonthlyPrice)
                .ThenBy(t => t.Name)
                .ToListAsync();
            return View(tiers);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Name,MonthlyPrice,DiscountPercent,FreeDelivery,MaxFamilyAccounts,HasDedicatedPharmacist,PriorityDispensing,IsActive")] MembershipTier tier)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tier);
                await _context.SaveChangesAsync();
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "Admin",
                    UserRole = "Admin",
                    Action = "TIER_CREATED",
                    Module = "Membership Tiers",
                    Details = $"Created membership tier '{tier.Name}' (₱{tier.MonthlyPrice:N2}/mo, {tier.DiscountPercent}% discount)."
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Membership tier '{tier.Name}' created.";
                return RedirectToAction(nameof(Index));
            }
            return View(tier);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var tier = await _context.MembershipTiers.FindAsync(id);
            if (tier == null) return NotFound();
            return View(tier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,Name,MonthlyPrice,DiscountPercent,FreeDelivery,MaxFamilyAccounts,HasDedicatedPharmacist,PriorityDispensing,IsActive")] MembershipTier tier)
        {
            if (id != tier.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(tier);
                await _context.SaveChangesAsync();
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "Admin",
                    UserRole = "Admin",
                    Action = "TIER_UPDATED",
                    Module = "Membership Tiers",
                    Details = $"Updated membership tier '{tier.Name}' (₱{tier.MonthlyPrice:N2}/mo, {tier.DiscountPercent}% discount)."
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Membership tier '{tier.Name}' updated.";
                return RedirectToAction(nameof(Index));
            }
            return View(tier);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var tier = await _context.MembershipTiers.Include(t => t.Memberships).FirstOrDefaultAsync(t => t.Id == id);
            if (tier == null) return NotFound();
            return View(tier);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tier = await _context.MembershipTiers.Include(t => t.Memberships).FirstOrDefaultAsync(t => t.Id == id);
            if (tier != null)
            {
                if (tier.Memberships.Any())
                {
                    TempData["Error"] = $"Cannot delete '{tier.Name}' — {tier.Memberships.Count} enrolled customer(s) are on this tier.";
                    return RedirectToAction(nameof(Index));
                }
                _context.MembershipTiers.Remove(tier);
                await _context.SaveChangesAsync();
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "Admin",
                    UserRole = "Admin",
                    Action = "TIER_DELETED",
                    Module = "Membership Tiers",
                    Details = $"Deleted membership tier '{tier.Name}'."
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Membership tier '{tier.Name}' deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}