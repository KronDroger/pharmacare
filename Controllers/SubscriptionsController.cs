using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using CarePlusPharmacy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Subscription / Auto-Refill module.
    // Admin: manage plans (CRUD).
    // Customer: browse plans, subscribe, view/cancel own subscriptions.
    // Admin/Cashier: process due refills (generates Sale + Billing, matching normal POS flow).
    [Authorize]
    public class SubscriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentCustomerService _currentCustomerService;

        public SubscriptionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ICurrentCustomerService currentCustomerService)
        {
            _context = context;
            _userManager = userManager;
            _currentCustomerService = currentCustomerService;
        }

        // ---------- ADMIN: PLAN MANAGEMENT ----------

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Plans(int page = 1, int pageSize = 10)
        {
            var query = _context.SubscriptionPlans
                .Include(p => p.Medicine)
                .Include(p => p.Subscriptions)
                .OrderBy(p => p.Name);
            var plans = await PaginatedList<SubscriptionPlan>.CreateAsync(query, page, pageSize);
            return View(plans);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult PlanCreate()
        {
            ViewBag.Medicines = _context.Medicines.OrderBy(m => m.Name).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PlanCreate([Bind("Name,MedicineId,Quantity,IntervalDays,Price,IsActive")] SubscriptionPlan plan)
        {
            if (ModelState.IsValid)
            {
                _context.Add(plan);
                await _context.SaveChangesAsync();
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "Admin",
                    UserRole = "Admin",
                    Action = "PLAN_CREATED",
                    Module = "Refill Plans",
                    Details = $"Created refill plan '{plan.Name}' (medicine #{plan.MedicineId}, {plan.Quantity} unit(s) every {plan.IntervalDays} days, ₱{plan.Price:N2})."
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Refill plan created.";
                return RedirectToAction(nameof(Plans));
            }
            ViewBag.Medicines = _context.Medicines.OrderBy(m => m.Name).ToList();
            return View(plan);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PlanEdit(int? id)
        {
            if (id == null) return NotFound();
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan == null) return NotFound();
            ViewBag.Medicines = _context.Medicines.OrderBy(m => m.Name).ToList();
            return View(plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PlanEdit(int id, [Bind("Id,Name,MedicineId,Quantity,IntervalDays,Price,IsActive")] SubscriptionPlan plan)
        {
            if (id != plan.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(plan);
                await _context.SaveChangesAsync();
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "Admin",
                    UserRole = "Admin",
                    Action = "PLAN_UPDATED",
                    Module = "Refill Plans",
                    Details = $"Updated refill plan '{plan.Name}' ({plan.Quantity} unit(s) every {plan.IntervalDays} days, ₱{plan.Price:N2}, active: {plan.IsActive})."
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Refill plan updated.";
                return RedirectToAction(nameof(Plans));
            }
            ViewBag.Medicines = _context.Medicines.OrderBy(m => m.Name).ToList();
            return View(plan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PlanDelete(int id)
        {
            var plan = await _context.SubscriptionPlans.Include(p => p.Subscriptions).FirstOrDefaultAsync(p => p.Id == id);
            if (plan != null)
            {
                int enrolled = plan.Subscriptions.Count;
                if (enrolled > 0)
                {
                    TempData["Error"] = $"Cannot delete '{plan.Name}' — {enrolled} customer subscription(s) reference this plan. Deactivate it instead; existing subscriptions stay intact.";
                    return RedirectToAction(nameof(Plans));
                }

                _context.SubscriptionPlans.Remove(plan);
                await _context.SaveChangesAsync();
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = User.Identity?.Name ?? "Admin",
                    UserRole = "Admin",
                    Action = "PLAN_DELETED",
                    Module = "Refill Plans",
                    Details = $"Deleted refill plan '{plan.Name}'."
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Refill plan removed.";
            }
            return RedirectToAction(nameof(Plans));
        }

        // ---------- CUSTOMER: BROWSE & SUBSCRIBE ----------

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Browse()
        {
            var plans = await _context.SubscriptionPlans
                .Include(p => p.Medicine)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View(plans);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(int planId)
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null)
            {
                TempData["Error"] = "No customer profile is linked to your account yet. Ask the pharmacy to link your account to a Customer record.";
                return RedirectToAction(nameof(Browse));
            }

            var plan = await _context.SubscriptionPlans.FindAsync(planId);
            if (plan == null || !plan.IsActive) return NotFound();

            bool alreadySubscribed = await _context.CustomerSubscriptions.AnyAsync(s =>
                s.CustomerId == customer.Id && s.SubscriptionPlanId == planId && s.Status == SubscriptionStatus.Active);
            if (alreadySubscribed)
            {
                TempData["Error"] = "You're already subscribed to this plan.";
                return RedirectToAction(nameof(Browse));
            }

            // Default the pickup branch to the pharmacy's main branch; customers can
            // change it any time from the portal (Portal/Subscriptions).
            var pickupBranch = await _context.Branches
                .OrderBy(b => b.IsMainBranch ? 0 : 1)
                .ThenBy(b => b.Id)
                .FirstOrDefaultAsync();

            var subscription = new CustomerSubscription
            {
                CustomerId = customer.Id,
                SubscriptionPlanId = plan.Id,
                StartDate = DateTime.Today,
                NextRefillDate = DateTime.Today.AddDays(plan.IntervalDays),
                Status = SubscriptionStatus.Active,
                PaymentMethod = "Cash",
                PickupBranchId = pickupBranch?.Id
            };
            _context.Add(subscription);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Subscribed to {plan.Name}. First refill due {subscription.NextRefillDate:yyyy-MM-dd}.";
            return RedirectToAction("Subscriptions", "Portal");
        }

        [Authorize(Roles = "Customer")]
        public IActionResult MySubscriptions()
        {
            // Customer subscription management now lives in the portal.
            return RedirectToAction("Subscriptions", "Portal");
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            var sub = await _context.CustomerSubscriptions.FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customer!.Id);
            if (sub != null)
            {
                sub.Status = SubscriptionStatus.Cancelled;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Subscription cancelled.";
            }
            return RedirectToAction("Subscriptions", "Portal");
        }

        // ---------- ADMIN/CASHIER: DUE REFILLS ----------

        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> DueRefills(int page = 1, int pageSize = 10)
        {
            var query = _context.CustomerSubscriptions
                .Include(s => s.Customer)
                .Include(s => s.SubscriptionPlan).ThenInclude(p => p!.Medicine)
                .Where(s => s.Status == SubscriptionStatus.Active && s.NextRefillDate <= DateTime.Today)
                .OrderBy(s => s.NextRefillDate)
                .ThenBy(s => s.Id);
            var due = await PaginatedList<CustomerSubscription>.CreateAsync(query, page, pageSize);
            return View(due);
        }

        // Generates a Sale + SaleDetail + Billing for the refill, mirroring a normal POS sale,
        // then advances NextRefillDate by the plan's interval.
        [Authorize(Roles = "Admin,Cashier")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefill(int id)
        {
            var sub = await _context.CustomerSubscriptions
                .Include(s => s.SubscriptionPlan).ThenInclude(p => p!.Medicine)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sub == null || sub.SubscriptionPlan == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var plan = sub.SubscriptionPlan;

            var sale = new Sale
            {
                CustomerId = sub.CustomerId,
                CashierId = userId,
                PaymentMethod = sub.PaymentMethod,
                SaleDate = DateTime.Today,
                Details = new List<SaleDetail>
                {
                    new SaleDetail
                    {
                        MedicineId = plan.MedicineId,
                        Quantity = plan.Quantity,
                        UnitPrice = plan.Price / plan.Quantity
                    }
                }
            };
            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            var billing = new Billing
            {
                SaleId = sale.Id,
                InvoiceNumber = $"SUB-{sale.Id:D6}",
                AmountDue = sale.TotalAmount,
                AmountPaid = sale.TotalAmount,
                PaymentMethod = sub.PaymentMethod,
                PaymentStatus = PaymentStatus.Paid,
                DateIssued = DateTime.Today
            };
            _context.Billings.Add(billing);

            sub.NextRefillDate = sub.NextRefillDate.AddDays(plan.IntervalDays);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Refill processed. Invoice {billing.InvoiceNumber} generated. Next refill: {sub.NextRefillDate:yyyy-MM-dd}.";
            return RedirectToAction(nameof(DueRefills));
        }

        // ---------- HELPERS ----------

        // Resolves the logged-in ApplicationUser's linked Customer record via the
        // ApplicationUser.CustomerId FK (see ICurrentCustomerService) — never by email.
    }
}
