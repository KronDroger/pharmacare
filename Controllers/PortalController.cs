using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using CarePlusPharmacy.Models.ViewModels;
using CarePlusPharmacy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Customer self-service portal. Every read/change is scoped to the Customer
    // profile explicitly linked to the signed-in account (ApplicationUser.CustomerId),
    // so a patient can only ever see and modify their own data.
    [Authorize(Roles = "Customer")]
    public class PortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentCustomerService _currentCustomerService;

        public PortalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ICurrentCustomerService currentCustomerService)
        {
            _context = context;
            _userManager = userManager;
            _currentCustomerService = currentCustomerService;
        }

        // ---------- DASHBOARD ----------

        public async Task<IActionResult> Index()
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null)
            {
                TempData["Error"] = "No patient profile is linked to your account yet. Ask the pharmacy to link your account so you can use the portal.";
                return View(new PortalDashboardViewModel());
            }

            var vm = new PortalDashboardViewModel
            {
                Customer = customer,
                ActiveSubscriptionCount = await _context.CustomerSubscriptions
                    .CountAsync(s => s.CustomerId == customer.Id && s.Status == SubscriptionStatus.Active),
                PausedSubscriptionCount = await _context.CustomerSubscriptions
                    .CountAsync(s => s.CustomerId == customer.Id && s.Status == SubscriptionStatus.Paused),
                PurchaseCount = await _context.Sales.CountAsync(s => s.CustomerId == customer.Id),
                PendingPrescriptionCount = await _context.Prescriptions
                    .CountAsync(p => p.CustomerId == customer.Id && p.Status == PrescriptionStatus.Pending),
                RecentPurchases = await _context.Sales
                    .Include(s => s.Details).ThenInclude(d => d.Medicine)
                    .Include(s => s.Billing)
                    .Where(s => s.CustomerId == customer.Id)
                    .OrderByDescending(s => s.SaleDate)
                    .Take(5)
                    .ToListAsync(),
                ActiveSubscriptions = await _context.CustomerSubscriptions
                    .Include(s => s.SubscriptionPlan).ThenInclude(p => p!.Medicine)
                    .Include(s => s.Branch)
                    .Where(s => s.CustomerId == customer.Id && s.Status == SubscriptionStatus.Active)
                    .OrderBy(s => s.NextRefillDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(vm);
        }

        // ---------- PROFILE ----------

        public async Task<IActionResult> Profile()
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile([Bind("FullName,Phone,Address,City,DateOfBirth,Gender")] Customer input)
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null)
            {
                TempData["Error"] = "No patient profile is linked to your account yet. Ask the pharmacy to link your account.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                customer.FullName = input.FullName.Trim();
                customer.Phone = input.Phone.Trim();
                customer.Address = string.IsNullOrWhiteSpace(input.Address) ? null : input.Address.Trim();
                customer.City = string.IsNullOrWhiteSpace(input.City) ? null : input.City.Trim();
                customer.DateOfBirth = input.DateOfBirth;
                customer.Gender = string.IsNullOrWhiteSpace(input.Gender) ? null : input.Gender.Trim();
                await _context.SaveChangesAsync();
                TempData["Success"] = "Your profile has been updated.";
                return RedirectToAction(nameof(Profile));
            }

            return View(customer);
        }

        // ---------- PURCHASES ----------

        public async Task<IActionResult> Purchases(string? search, string? paymentMethod, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null)
            {
                var empty = new PaginatedList<Sale>(new List<Sale>(), 0, 1, pageSize);
                return View(empty);
            }

            var baseQuery = _context.Sales.Where(s => s.CustomerId == customer.Id);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                baseQuery = baseQuery.Where(s =>
                    s.Id.ToString().Contains(search)
                    || (s.Billing != null && s.Billing.InvoiceNumber != null && s.Billing.InvoiceNumber.Contains(search))
                    || s.Details.Any(d => d.Medicine != null && d.Medicine.Name.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                baseQuery = baseQuery.Where(s => s.PaymentMethod == paymentMethod);
            }

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                baseQuery = baseQuery.Where(s => s.SaleDate >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date.AddDays(1);
                baseQuery = baseQuery.Where(s => s.SaleDate < toDate);
            }

            ViewBag.Search = search;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.FilteredCount = await baseQuery.CountAsync();
            ViewBag.FilteredSpent = (await baseQuery
                .Select(s => (decimal?)s.TotalAmount)
                .ToListAsync()).Sum(s => s ?? 0m);

            var query = baseQuery
                .Include(s => s.Details).ThenInclude(d => d.Medicine)
                .Include(s => s.Billing)
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id);

            var sales = await PaginatedList<Sale>.CreateAsync(query, page, pageSize);
            return View(sales);
        }

        public async Task<IActionResult> PurchaseDetails(int? id)
        {
            if (id == null) return NotFound();
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null) return NotFound();

            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Cashier)
                .Include(s => s.Details).ThenInclude(d => d.Medicine)
                .Include(s => s.Details).ThenInclude(d => d.Batch)
                .Include(s => s.Billing)
                .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customer.Id);

            if (sale == null) return NotFound();
            return View(sale);
        }

        // ---------- PRESCRIPTIONS ----------

        public async Task<IActionResult> Prescriptions()
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null) return View(new List<Prescription>());

            var list = await _context.Prescriptions
                .Include(p => p.Details).ThenInclude(d => d.Medicine)
                .Where(p => p.CustomerId == customer.Id)
                .OrderByDescending(p => p.DatePrescribed)
                .ThenByDescending(p => p.Id)
                .ToListAsync();
            return View(list);
        }

        // ---------- REWARDS ----------

        public async Task<IActionResult> Rewards()
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null) return View(new PortalRewardsViewModel());

            var vm = new PortalRewardsViewModel
            {
                Customer = customer,
                Transactions = await _context.Sales
                    .Include(s => s.Details).ThenInclude(d => d.Medicine)
                    .Where(s => s.CustomerId == customer.Id && (s.PointsEarned > 0 || s.PointsRedeemed > 0))
                    .OrderByDescending(s => s.SaleDate)
                    .ThenByDescending(s => s.Id)
                    .Take(50)
                    .ToListAsync()
            };
            return View(vm);
        }

        // ---------- SUBSCRIPTIONS ----------

        public async Task<IActionResult> Subscriptions(int page = 1, int pageSize = 10)
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null)
            {
                var empty = new PaginatedList<CustomerSubscription>(new List<CustomerSubscription>(), 0, 1, pageSize);
                ViewBag.Branches = await _context.Branches.OrderBy(b => b.Name).ToListAsync();
                return View(empty);
            }

            ViewBag.Branches = await _context.Branches.OrderBy(b => b.Name).ToListAsync();
            var query = _context.CustomerSubscriptions
                .Include(s => s.SubscriptionPlan).ThenInclude(p => p!.Medicine)
                .Include(s => s.Branch)
                .Where(s => s.CustomerId == customer.Id)
                .OrderByDescending(s => s.Status == SubscriptionStatus.Active)
                .ThenBy(s => s.NextRefillDate);

            var subs = await PaginatedList<CustomerSubscription>.CreateAsync(query, page, pageSize);
            return View(subs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pause(int id)
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null) return NotFound();

            var sub = await _context.CustomerSubscriptions.FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customer.Id);
            if (sub == null) return NotFound();

            if (sub.Status == SubscriptionStatus.Active)
            {
                sub.Status = SubscriptionStatus.Paused;
                await _context.SaveChangesAsync();
                await LogCustomerActionAsync("SUBSCRIPTION_PAUSED", $"Subscription #{sub.Id} paused by customer.");
                TempData["Success"] = "Subscription paused.";
            }
            return RedirectToAction(nameof(Subscriptions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resume(int id)
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null) return NotFound();

            var sub = await _context.CustomerSubscriptions.FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customer.Id);
            if (sub == null) return NotFound();

            if (sub.Status == SubscriptionStatus.Paused)
            {
                sub.Status = SubscriptionStatus.Active;
                await _context.SaveChangesAsync();
                await LogCustomerActionAsync("SUBSCRIPTION_RESUMED", $"Subscription #{sub.Id} resumed by customer.");
                TempData["Success"] = "Subscription resumed.";
            }
            return RedirectToAction(nameof(Subscriptions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPickupBranch(int id, int? pickupBranchId)
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null) return NotFound();

            var sub = await _context.CustomerSubscriptions.FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customer.Id);
            if (sub == null) return NotFound();

            var branch = pickupBranchId.HasValue ? await _context.Branches.FindAsync(pickupBranchId.Value) : null;
            if (branch == null)
            {
                TempData["Error"] = "Please choose a valid pickup branch.";
                return RedirectToAction(nameof(Subscriptions));
            }

            sub.PickupBranchId = branch.Id;
            await _context.SaveChangesAsync();
            await LogCustomerActionAsync("SUBSCRIPTION_PICKUP_CHANGED", $"Subscription #{sub.Id} pickup moved to branch '{branch.Name}'.");
            TempData["Success"] = $"Pickup branch updated to {branch.Name}.";
            return RedirectToAction(nameof(Subscriptions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null) return NotFound();

            var sub = await _context.CustomerSubscriptions.FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customer.Id);
            if (sub == null) return NotFound();

            if (sub.Status != SubscriptionStatus.Cancelled)
            {
                sub.Status = SubscriptionStatus.Cancelled;
                await _context.SaveChangesAsync();
                await LogCustomerActionAsync("SUBSCRIPTION_CANCELLED", $"Subscription #{sub.Id} cancelled by customer.");
                TempData["Success"] = "Subscription cancelled.";
            }
            return RedirectToAction(nameof(Subscriptions));
        }

        // ---------- HELPERS ----------

        private async Task LogCustomerActionAsync(string action, string details)
        {
            var user = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                Timestamp = DateTime.Now,
                UserId = user?.Id,
                UserName = user?.FullName ?? user?.UserName ?? "Patient",
                UserRole = "Customer",
                Action = action,
                Module = "Patient Portal",
                Details = details,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
        }
    }
}