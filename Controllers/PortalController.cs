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
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ICurrentCustomerService _currentCustomerService;

        public PortalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, ICurrentCustomerService currentCustomerService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
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

        // ---------- SECURITY (change password) ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["SecurityError"] = "Please fill in both your current and new password.";
                return RedirectToAction(nameof(Profile));
            }
            if (newPassword != confirmPassword)
            {
                TempData["SecurityError"] = "New password and confirmation do not match.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Profile));

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                TempData["SecurityError"] = string.Join(" ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Profile));
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["SecuritySuccess"] = "Your password has been changed.";
            return RedirectToAction(nameof(Profile));
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

            var baseQuery = _context.Sales.Where(s => s.CustomerId == customer.Id && !s.IsVoided);

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
            ViewBag.MembershipFreeDelivery = (await GetActiveMembershipTierAsync(customer.Id))?.FreeDelivery == true;
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
            ViewBag.MembershipPriorityDispensing = (await GetActiveMembershipTierAsync(customer.Id))?.PriorityDispensing == true;
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

        // ---------- MEMBERSHIP ----------

        public async Task<IActionResult> Membership()
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null)
            {
                TempData["Error"] = "No patient profile is linked to your account yet. Ask the pharmacy to link your account so you can use the portal.";
                return View(new PortalMembershipViewModel());
            }

            var memberships = await _context.CustomerMemberships
                .Include(m => m.MembershipTier)
                .Where(m => m.CustomerId == customer.Id)
                .OrderByDescending(m => m.StartDate)
                .ThenByDescending(m => m.Id)
                .ToListAsync();

            var vm = new PortalMembershipViewModel
            {
                Customer = customer,
                CurrentMembership = memberships.FirstOrDefault(m => m.Status == MembershipStatus.Active),
                Tiers = await _context.MembershipTiers
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.MonthlyPrice)
                    .ThenBy(t => t.Name)
                    .ToListAsync(),
                History = memberships
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(int id)
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null)
            {
                TempData["Error"] = "No patient profile is linked to your account yet. Ask the pharmacy to link your account.";
                return RedirectToAction(nameof(Membership));
            }

            var tier = await _context.MembershipTiers.FindAsync(id);
            if (tier == null || !tier.IsActive)
            {
                TempData["Error"] = "That membership tier is not available right now.";
                return RedirectToAction(nameof(Membership));
            }

            var current = await _context.CustomerMemberships
                .FirstOrDefaultAsync(m => m.CustomerId == customer.Id && m.Status == MembershipStatus.Active);

            var switched = false;
            if (current != null)
            {
                if (current.MembershipTierId == tier.Id)
                {
                    TempData["Success"] = $"You are already subscribed to {tier.Name}.";
                    return RedirectToAction(nameof(Membership));
                }
                current.Status = MembershipStatus.Cancelled;
                switched = true;
            }

            _context.CustomerMemberships.Add(new CustomerMembership
            {
                CustomerId = customer.Id,
                MembershipTierId = tier.Id,
                StartDate = DateTime.Today,
                NextBillingDate = DateTime.Today.AddMonths(1),
                Status = MembershipStatus.Active,
                PaymentMethod = "Portal"
            });
            await _context.SaveChangesAsync();
            await LogCustomerActionAsync("MEMBERSHIP_SUBSCRIBED", $"Customer subscribed to tier '{tier.Name}' (₱{tier.MonthlyPrice:N2}/month).");
            TempData["Success"] = switched
                ? $"Membership switched to {tier.Name}."
                : $"Welcome to {tier.Name}! Your membership is now active.";
            return RedirectToAction(nameof(Membership));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelMembership(int id)
        {
            var customer = await _currentCustomerService.GetCurrentCustomerAsync(User);
            if (customer == null)
            {
                TempData["Error"] = "No patient profile is linked to your account yet. Ask the pharmacy to link your account.";
                return RedirectToAction(nameof(Membership));
            }

            var membership = await _context.CustomerMemberships
                .Include(m => m.MembershipTier)
                .FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customer.Id);
            if (membership == null) return NotFound();

            if (membership.Status == MembershipStatus.Active)
            {
                membership.Status = MembershipStatus.Cancelled;
                await _context.SaveChangesAsync();
                await LogCustomerActionAsync("MEMBERSHIP_CANCELLED", $"Customer cancelled membership tier '{membership.MembershipTier?.Name}'.");
                TempData["Success"] = "Your membership has been cancelled.";
            }
            return RedirectToAction(nameof(Membership));
        }

        // ---------- HELPERS ----------

        private async Task<MembershipTier?> GetActiveMembershipTierAsync(int customerId)
            => (await _context.CustomerMemberships
                .Include(m => m.MembershipTier)
                .Where(m => m.CustomerId == customerId && m.Status == MembershipStatus.Active)
                .OrderByDescending(m => m.StartDate)
                .ThenByDescending(m => m.Id)
                .FirstOrDefaultAsync())?.MembershipTier;

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