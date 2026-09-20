using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
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

        public SubscriptionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                TempData["Success"] = "Subscription plan created.";
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
                TempData["Success"] = "Subscription plan updated.";
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
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan != null)
            {
                _context.SubscriptionPlans.Remove(plan);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Subscription plan removed.";
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
            var customer = await CurrentCustomerAsync();
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

            var subscription = new CustomerSubscription
            {
                CustomerId = customer.Id,
                SubscriptionPlanId = plan.Id,
                StartDate = DateTime.Today,
                NextRefillDate = DateTime.Today.AddDays(plan.IntervalDays),
                Status = SubscriptionStatus.Active,
                PaymentMethod = "Cash"
            };
            _context.Add(subscription);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Subscribed to {plan.Name}. First refill due {subscription.NextRefillDate:yyyy-MM-dd}.";
            return RedirectToAction(nameof(MySubscriptions));
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MySubscriptions(int page = 1, int pageSize = 10)
        {
            var customer = await CurrentCustomerAsync();
            if (customer == null)
            {
                var empty = new PaginatedList<CustomerSubscription>(new List<CustomerSubscription>(), 0, 1, pageSize);
                return View(empty);
            }

            var query = _context.CustomerSubscriptions
                .Include(s => s.SubscriptionPlan).ThenInclude(p => p!.Medicine)
                .Where(s => s.CustomerId == customer.Id)
                .OrderByDescending(s => s.Status == SubscriptionStatus.Active)
                .ThenBy(s => s.NextRefillDate);

            var subs = await PaginatedList<CustomerSubscription>.CreateAsync(query, page, pageSize);
            return View(subs);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var customer = await CurrentCustomerAsync();
            var sub = await _context.CustomerSubscriptions.FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customer!.Id);
            if (sub != null)
            {
                sub.Status = SubscriptionStatus.Cancelled;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Subscription cancelled.";
            }
            return RedirectToAction(nameof(MySubscriptions));
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

        // Matches the logged-in ApplicationUser to a Customer record by email.
        // Assumes the pharmacy creates the Customer record with the same email used for the account.
        private async Task<Customer?> CurrentCustomerAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Email == null) return null;
            return await _context.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);
        }
    }
}
