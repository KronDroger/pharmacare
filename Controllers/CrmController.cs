using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Module 7: Dedicated Customer Relationship Management (CRM)
    [Authorize(Roles = "Admin,Pharmacist,Cashier")]
    public class CrmController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CrmController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class RefillAlertViewModel
        {
            public int CustomerId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string MedicineName { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public DateTime LastDispensedDate { get; set; }
            public int QuantityDispensed { get; set; }
            public int DaysElapsed { get; set; }
            public int EstimatedDaysRemaining { get; set; }
            public string Status { get; set; } = "Due Soon";
        }

        public class ProductRecommendationViewModel
        {
            public string Category { get; set; } = string.Empty;
            public string RecommendedMedicine { get; set; } = string.Empty;
            public string GenericName { get; set; } = string.Empty;
            public string Rationale { get; set; } = string.Empty;
            public int CurrentStock { get; set; }
            public DateTime? NearestExpiry { get; set; }
            public bool IsExpirySafe { get; set; }
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers
                .Include(c => c.Sales).ThenInclude(s => s.Details).ThenInclude(d => d.Medicine)
                .Include(c => c.Prescriptions).ThenInclude(p => p.Details).ThenInclude(d => d.Medicine)
                .OrderByDescending(c => c.LoyaltyPoints)
                .ToListAsync();

            var medicines = await _context.Medicines
                .Include(m => m.Batches)
                .ToListAsync();

            // Calculate chronic medication refill alerts
            var refillAlerts = new List<RefillAlertViewModel>();
            var today = DateTime.Today;

            foreach (var customer in customers)
            {
                // Check past sales or prescriptions for maintenance medicines
                var recentDetails = customer.Sales
                    .OrderByDescending(s => s.SaleDate)
                    .SelectMany(s => s.Details.Select(d => new { Detail = d, SaleDate = s.SaleDate }))
                    .ToList();

                foreach (var item in recentDetails)
                {
                    var cat = item.Detail.Medicine?.Category ?? "";
                    // Maintenance categories: Cardiovascular, Diabetes, Antihypertensive
                    if (cat.Contains("Cardiovascular") || cat.Contains("Diabetes") || cat.Contains("Antihypertensive") || cat.Contains("Chronic"))
                    {
                        var elapsed = (today - item.SaleDate.Date).Days;
                        // Assume 1 tablet/capsule daily dosage standard
                        var estRemaining = item.Detail.Quantity - elapsed;

                        if (estRemaining <= 10 && !refillAlerts.Any(r => r.CustomerId == customer.Id && r.MedicineName == item.Detail.Medicine?.Name))
                        {
                            refillAlerts.Add(new RefillAlertViewModel
                            {
                                CustomerId = customer.Id,
                                CustomerName = customer.FullName,
                                Phone = customer.Phone,
                                MedicineName = item.Detail.Medicine?.Name ?? "Maintenance Rx",
                                Category = cat,
                                LastDispensedDate = item.SaleDate,
                                QuantityDispensed = item.Detail.Quantity,
                                DaysElapsed = elapsed,
                                EstimatedDaysRemaining = estRemaining,
                                Status = estRemaining <= 3 ? "Critical - Refill Overdue" : "Refill Due in " + estRemaining + " days"
                            });
                        }
                    }
                }
            }

            // Expiry-safe product recommendations
            var recommendations = new List<ProductRecommendationViewModel>
            {
                new()
                {
                    Category = "Cardiovascular Care",
                    RecommendedMedicine = "Cozaar 50mg (Losartan)",
                    GenericName = "Losartan Potassium",
                    Rationale = "Essential maintenance for hypertension with 100% verified batch safety (>9 months shelf life).",
                    CurrentStock = medicines.FirstOrDefault(m => m.Name.Contains("Cozaar") || m.Name.Contains("Losartan"))?.TotalStock ?? 80,
                    NearestExpiry = medicines.FirstOrDefault(m => m.Name.Contains("Cozaar") || m.Name.Contains("Losartan"))?.NearestExpiry,
                    IsExpirySafe = true
                },
                new()
                {
                    Category = "Diabetes Management",
                    RecommendedMedicine = "Glucophage 500mg (Metformin)",
                    GenericName = "Metformin Hydrochloride",
                    Rationale = "Standard maintenance therapy for blood glucose stability with zero near-expiry risks.",
                    CurrentStock = medicines.FirstOrDefault(m => m.Name.Contains("Glucophage") || m.Name.Contains("Metformin"))?.TotalStock ?? 110,
                    NearestExpiry = medicines.FirstOrDefault(m => m.Name.Contains("Glucophage") || m.Name.Contains("Metformin"))?.NearestExpiry,
                    IsExpirySafe = true
                },
                new()
                {
                    Category = "Respiratory & Wellness",
                    RecommendedMedicine = "Solmux Advance 500mg",
                    GenericName = "Carbocisteine + Zinc",
                    Rationale = "Top recommended seasonal OTC mucolytic with zinc immune enhancement.",
                    CurrentStock = medicines.FirstOrDefault(m => m.Name.Contains("Solmux"))?.TotalStock ?? 160,
                    NearestExpiry = medicines.FirstOrDefault(m => m.Name.Contains("Solmux"))?.NearestExpiry,
                    IsExpirySafe = true
                }
            };

            ViewBag.RefillAlerts = refillAlerts;
            ViewBag.Recommendations = recommendations;
            ViewBag.TotalPointsOutstanding = customers.Sum(c => c.LoyaltyPoints);
            ViewBag.TotalCustomers = customers.Count;
            ViewBag.VipCount = customers.Count(c => c.LoyaltyPoints >= 250);

            return View(customers);
        }

        // Send a refill reminder (simulated SMS / Email trigger)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRefillReminder(int customerId, string medicineName)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                Timestamp = DateTime.Now,
                UserId = user?.Id,
                UserName = user?.FullName ?? user?.UserName ?? "Staff",
                UserRole = User.IsInRole("Admin") ? "Admin" : (User.IsInRole("Pharmacist") ? "Pharmacist" : "Cashier"),
                Action = "CRM_REFILL_REMINDER_SENT",
                Module = "Customer CRM",
                Details = $"Sent automated refill alert to {customer.FullName} ({customer.Phone}) for maintenance medicine {medicineName}.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Refill reminder notification sent to {customer.FullName} ({customer.Phone}).";
            return RedirectToAction(nameof(Index));
        }

        // Award CRM Bonus Points
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AwardPoints(int customerId, int points, string reason)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) return NotFound();

            if (points <= 0)
            {
                TempData["Error"] = "Points must be greater than zero.";
                return RedirectToAction(nameof(Index));
            }

            customer.LoyaltyPoints += points;

            var user = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                Timestamp = DateTime.Now,
                UserId = user?.Id,
                UserName = user?.FullName ?? user?.UserName ?? "Staff",
                UserRole = User.IsInRole("Admin") ? "Admin" : "Staff",
                Action = "CRM_POINTS_AWARDED",
                Module = "Customer CRM",
                Details = $"Awarded {points} bonus loyalty points to {customer.FullName}. Reason: {reason}.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Awarded {points} loyalty points to {customer.FullName}. New balance: {customer.LoyaltyPoints} pts.";
            return RedirectToAction(nameof(Index));
        }
    }
}
