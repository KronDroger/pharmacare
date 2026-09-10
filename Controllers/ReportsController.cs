using CarePlusPharmacy.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Module 10: Reports & Analytics Module (Admin and Pharmacist)
    [Authorize(Roles = "Admin,Pharmacist")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ReportsController(ApplicationDbContext context) => _context = context;

        public class TopSellerReportItem
        {
            public string MedicineName { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public int UnitsSold { get; set; }
            public decimal Revenue { get; set; }
        }

        public class TopCustomerReportItem
        {
            public string CustomerName { get; set; } = string.Empty;
            public string Tier { get; set; } = string.Empty;
            public int OrdersCount { get; set; }
            public decimal TotalSpent { get; set; }
            public int LoyaltyPoints { get; set; }
        }

        public async Task<IActionResult> Index()
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Details).ThenInclude(d => d.Medicine)
                .ToListAsync();

            var medicines = await _context.Medicines
                .Include(m => m.Batches)
                .ToListAsync();

            var purchaseOrders = await _context.PurchaseOrders
                .Include(p => p.Details)
                .ToListAsync();

            var customers = await _context.Customers
                .Include(c => c.Sales)
                .ToListAsync();

            // Financial KPIs
            decimal totalRevenue = sales.Sum(s => s.TotalAmount);
            decimal inventoryValuation = medicines.Sum(m => m.TotalStock * m.UnitPrice);
            decimal purchasingCost = purchaseOrders.SelectMany(p => p.Details).Sum(d => d.Quantity * d.UnitCost);
            decimal estimatedGrossProfit = Math.Max(0, totalRevenue - (purchasingCost > 0 ? (purchasingCost * 0.6m) : 0));

            // Expiry risk valuation
            var today = DateTime.Today;
            decimal expiredValue = medicines.SelectMany(m => m.Batches)
                .Where(b => b.ExpiryDate < today && b.Quantity > 0)
                .Sum(b => b.Quantity * (b.Medicine?.UnitPrice ?? 0));

            decimal nearExpiryRisk = medicines.SelectMany(m => m.Batches)
                .Where(b => b.ExpiryDate >= today && b.ExpiryDate <= today.AddDays(90) && b.Quantity > 0)
                .Sum(b => b.Quantity * (b.Medicine?.UnitPrice ?? 0));

            // Monthly Sales Trend
            var salesByMonth = sales
                .GroupBy(s => s.SaleDate.ToString("yyyy-MM"))
                .OrderBy(g => g.Key)
                .Select(g => new { Month = g.Key, Total = g.Sum(s => s.TotalAmount) })
                .ToList();

            // Top-Selling Medicines
            var topMeds = sales.SelectMany(s => s.Details)
                .GroupBy(d => d.Medicine?.Name ?? "Unknown")
                .Select(g => new TopSellerReportItem
                {
                    MedicineName = g.Key,
                    Category = g.FirstOrDefault()?.Medicine?.Category ?? "General",
                    UnitsSold = g.Sum(d => d.Quantity),
                    Revenue = g.Sum(d => d.Quantity * d.UnitPrice)
                })
                .OrderByDescending(t => t.Revenue)
                .Take(6)
                .ToList();

            // Top Spending Customers (CRM)
            var topCustomers = customers
                .Select(c => new TopCustomerReportItem
                {
                    CustomerName = c.FullName,
                    Tier = c.Tier,
                    OrdersCount = c.Sales.Count,
                    TotalSpent = c.Sales.Sum(s => s.TotalAmount),
                    LoyaltyPoints = c.LoyaltyPoints
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(5)
                .ToList();

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.InventoryValue = inventoryValuation;
            ViewBag.TotalPurchasingCost = purchasingCost;
            ViewBag.GrossProfit = estimatedGrossProfit;
            ViewBag.ExpiredLoss = expiredValue;
            ViewBag.NearExpiryRisk = nearExpiryRisk;
            ViewBag.SalesByMonth = salesByMonth;
            ViewBag.TopMedicines = topMeds;
            ViewBag.TopCustomers = topCustomers;
            ViewBag.TotalTransactions = sales.Count;
            ViewBag.PointsCirculating = customers.Sum(c => c.LoyaltyPoints);

            return View();
        }
    }
}
