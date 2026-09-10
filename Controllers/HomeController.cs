using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // All authenticated staff roles can view the analytics dashboard.
    // Plain Customer accounts are redirected to their own order history instead.
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Customer"))
            {
                return RedirectToAction("Index", "Customers");
            }
            var medicines = await _context.Medicines.Include(m => m.Batches).ToListAsync();
            var sales = await _context.Sales
                .Include(s => s.Details).ThenInclude(d => d.Medicine)
                .Include(s => s.Customer)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
            var purchaseOrders = await _context.PurchaseOrders.ToListAsync();

            var vm = new DashboardViewModel
            {
                TotalStockUnits = medicines.Sum(m => m.TotalStock),
                LowStockCount = medicines.Count(m => m.TotalStock > 0 && m.TotalStock <= m.ReorderLevel),
                OutOfStockCount = medicines.Count(m => m.TotalStock == 0),
                ExpiringSoonCount = medicines.Count(m => m.NearestExpiry.HasValue
                    && m.NearestExpiry.Value >= DateTime.Today
                    && m.NearestExpiry.Value <= DateTime.Today.AddDays(45)),

                TotalRevenue = sales.Sum(s => s.TotalAmount),
                TotalUnitsSold = sales.SelectMany(s => s.Details).Sum(d => d.Quantity),
                InventoryValue = medicines.Sum(m => m.TotalStock * m.UnitPrice),
                PendingPurchaseOrders = purchaseOrders.Count(p => p.Status == Models.PurchaseOrderStatus.Pending),

                StockByCategory = medicines
                    .GroupBy(m => m.Category)
                    .Select(g => new CategoryStockViewModel { Category = g.Key, Stock = g.Sum(m => m.TotalStock) })
                    .ToList(),

                TopSellingMedicines = sales
                    .SelectMany(s => s.Details)
                    .GroupBy(d => d.Medicine!.Name)
                    .Select(g => new TopMedicineViewModel { MedicineName = g.Key, QuantitySold = g.Sum(d => d.Quantity) })
                    .OrderByDescending(t => t.QuantitySold)
                    .Take(5)
                    .ToList(),

                RecentSales = sales.Take(5).ToList()
            };

            return View(vm);
        }

        [AllowAnonymous]
        public IActionResult Error() => View();
    }
}
