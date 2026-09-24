using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Medicine Inventory module.
    // Admin + Inventory Coordinator: full CRUD.
    // Pharmacist: read-only (view use case, per the Use Case Diagram's dashed line).
    [Authorize(Roles = "Admin,InventoryCoordinator,Pharmacist")]
    public class MedicinesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public MedicinesController(ApplicationDbContext context) => _context = context;

        private bool CanEdit => User.IsInRole("Admin") || User.IsInRole("InventoryCoordinator");

        public async Task<IActionResult> Index(string? search, string? category, string? status, int page = 1, int pageSize = 10)
        {
            var query = _context.Medicines.Include(m => m.Supplier).Include(m => m.Batches).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(m =>
                    m.Name.Contains(search)
                    || m.Category.Contains(search)
                    || (m.GenericName != null && m.GenericName.Contains(search))
                    || (m.Manufacturer != null && m.Manufacturer.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(m => m.Category == category);
            }

            if (string.Equals(status, "LowStock", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.Batches.Sum(b => b.Quantity) > 0 && m.Batches.Sum(b => b.Quantity) <= m.ReorderLevel);
            }
            else if (string.Equals(status, "OutOfStock", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.Batches.Sum(b => b.Quantity) <= 0);
            }
            else if (string.Equals(status, "Optimal", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.Batches.Sum(b => b.Quantity) > m.ReorderLevel);
            }

            ViewBag.TotalCountAll = await query.CountAsync();
            ViewBag.TotalUnitsAll = await query.SelectMany(m => m.Batches).SumAsync(b => (int?)b.Quantity) ?? 0;
            ViewBag.LowStockCountAll = await query.CountAsync(m => m.Batches.Sum(b => b.Quantity) <= m.ReorderLevel);
            ViewBag.TotalValuationAll = await query.SelectMany(m => m.Batches).SumAsync(b => (decimal?)(b.Quantity * b.Medicine!.UnitPrice)) ?? 0m;

            ViewBag.Search = search;
            ViewBag.CanEdit = CanEdit;

            var categories = await _context.Medicines.Select(m => m.Category).Distinct().OrderBy(c => c).ToListAsync();
            ViewBag.Filters = new List<Models.ViewModels.FilterField>
            {
                new() { Name = "search", Label = "Search brand, generic, manufacturer", Type = Models.ViewModels.FilterFieldType.Text, Value = search },
                new() { Name = "category", Label = "Category", Type = Models.ViewModels.FilterFieldType.Select, Value = category,
                    Options = categories.Select(c => new Models.ViewModels.FilterOption { Value = c, Label = c }).ToList() },
                new() { Name = "status", Label = "Stock Status", Type = Models.ViewModels.FilterFieldType.Select, Value = status,
                    Options = new List<Models.ViewModels.FilterOption>
                    {
                        new() { Value = "Optimal", Label = "Optimal" },
                        new() { Value = "LowStock", Label = "Low Stock" },
                        new() { Value = "OutOfStock", Label = "Out of Stock" }
                    } }
            };

            var medicines = await PaginatedList<Medicine>.CreateAsync(query.OrderBy(m => m.Name), page, pageSize);
            return View(medicines);
        }

        // Expiry Monitoring — tracks batch-level expiration dates and financial risk
        public async Task<IActionResult> Expiring(string? tab, string? search, string? category, DateTime? from, DateTime? to, int page = 1, int pageSize = 10, bool print = false)
        {
            tab = string.IsNullOrWhiteSpace(tab) ? "all" : tab.ToLower();
            var today = DateTime.Today;

            var baseQuery = _context.MedicineBatches.Include(b => b.Medicine).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                baseQuery = baseQuery.Where(b =>
                    (b.Medicine != null && b.Medicine.Name.Contains(search))
                    || (b.Medicine != null && b.Medicine.GenericName != null && b.Medicine.GenericName.Contains(search))
                    || b.BatchNumber.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                baseQuery = baseQuery.Where(b => b.Medicine != null && b.Medicine.Category == category);
            }

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                baseQuery = baseQuery.Where(b => b.ExpiryDate >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date.AddDays(1);
                baseQuery = baseQuery.Where(b => b.ExpiryDate < toDate);
            }

            // Compute counts and valuations for ALL tabs over full scoped dataset
            ViewBag.TotalCountAll = await baseQuery.CountAsync();

            var expiredQuery = baseQuery.Where(b => b.ExpiryDate < today && b.Quantity > 0);
            ViewBag.ExpiredCount = await expiredQuery.CountAsync();
            ViewBag.ExpiredValuation = await expiredQuery.SumAsync(b => (decimal?)(b.Quantity * b.Medicine!.UnitPrice)) ?? 0m;

            var criticalQuery = baseQuery.Where(b => b.ExpiryDate >= today && b.ExpiryDate <= today.AddDays(30) && b.Quantity > 0);
            ViewBag.CriticalCount = await criticalQuery.CountAsync();
            ViewBag.CriticalValuation = await criticalQuery.SumAsync(b => (decimal?)(b.Quantity * b.Medicine!.UnitPrice)) ?? 0m;

            var warningQuery = baseQuery.Where(b => b.ExpiryDate > today.AddDays(30) && b.ExpiryDate <= today.AddDays(90) && b.Quantity > 0);
            ViewBag.WarningCount = await warningQuery.CountAsync();
            ViewBag.WarningValuation = await warningQuery.SumAsync(b => (decimal?)(b.Quantity * b.Medicine!.UnitPrice)) ?? 0m;

            var safeQuery = baseQuery.Where(b => b.ExpiryDate > today.AddDays(90) && b.Quantity > 0);
            ViewBag.SafeCount = await safeQuery.CountAsync();
            ViewBag.SafeValuation = await safeQuery.SumAsync(b => (decimal?)(b.Quantity * b.Medicine!.UnitPrice)) ?? 0m;

            // Filter by selected tab before paging
            var filteredQuery = tab switch
            {
                "expired" => expiredQuery,
                "critical" => criticalQuery,
                "warning" => warningQuery,
                "safe" => safeQuery,
                _ => baseQuery
            };

            ViewBag.CurrentTab = tab;
            ViewBag.Print = print;

            var categories = await _context.Medicines.Select(m => m.Category).Distinct().OrderBy(c => c).ToListAsync();
            ViewBag.Filters = new List<Models.ViewModels.FilterField>
            {
                new() { Name = "search", Label = "Search medicine or batch #", Type = Models.ViewModels.FilterFieldType.Text, Value = search },
                new() { Name = "category", Label = "Category", Type = Models.ViewModels.FilterFieldType.Select, Value = category,
                    Options = categories.Select(c => new Models.ViewModels.FilterOption { Value = c, Label = c }).ToList() },
                new() { Name = "from", Label = "Expiry From", Type = Models.ViewModels.FilterFieldType.Date, Value = from?.ToString("yyyy-MM-dd") },
                new() { Name = "to", Label = "Expiry To", Type = Models.ViewModels.FilterFieldType.Date, Value = to?.ToString("yyyy-MM-dd") }
            };

            if (print)
            {
                var allBatches = await filteredQuery.OrderBy(b => b.ExpiryDate).ThenBy(b => b.Id).ToListAsync();
                var paginatedList = new PaginatedList<MedicineBatch>(allBatches, allBatches.Count, 1, Math.Max(1, allBatches.Count));
                return View(paginatedList);
            }

            var batches = await PaginatedList<MedicineBatch>.CreateAsync(
                filteredQuery.OrderBy(b => b.ExpiryDate).ThenBy(b => b.Id),
                page,
                pageSize
            );

            return View(batches);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,InventoryCoordinator")]
        public async Task<IActionResult> QuarantineBatch(int id, string reason)
        {
            var batch = await _context.MedicineBatches.Include(b => b.Medicine).FirstOrDefaultAsync(b => b.Id == id);
            if (batch != null)
            {
                var qty = batch.Quantity;
                batch.Quantity = 0; // write off expired or spoiled units
                
                _context.AuditLogs.Add(new AuditLog
                {
                    Timestamp = DateTime.Now,
                    UserName = User.Identity?.Name ?? "Staff",
                    UserRole = User.IsInRole("Admin") ? "Admin" : "InventoryCoordinator",
                    Action = "BATCH_QUARANTINED",
                    Module = "Expiry Monitoring",
                    Details = $"Quarantined/Written-off Batch {batch.BatchNumber} ({batch.Medicine?.Name}, Qty: {qty}). Reason: {reason}."
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Batch {batch.BatchNumber} successfully quarantined and removed from active stock ({qty} units written off).";
            }
            return RedirectToAction(nameof(Expiring));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var medicine = await _context.Medicines
                .Include(m => m.Supplier).Include(m => m.Batches)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (medicine == null) return NotFound();
            return View(medicine);
        }

        [Authorize(Roles = "Admin,InventoryCoordinator")]
        public IActionResult Create()
        {
            ViewBag.Suppliers = _context.Suppliers.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,InventoryCoordinator")]
        public async Task<IActionResult> Create([Bind("Name,GenericName,Category,Manufacturer,Description,UnitPrice,ReorderLevel,SupplierId,IsVatExempt,RxRequired")] Medicine medicine)
        {
            if (ModelState.IsValid)
            {
                _context.Add(medicine);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Medicine added successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Suppliers = _context.Suppliers.ToList();
            return View(medicine);
        }

        [Authorize(Roles = "Admin,InventoryCoordinator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null) return NotFound();
            ViewBag.Suppliers = _context.Suppliers.ToList();
            return View(medicine);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,InventoryCoordinator")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,GenericName,Category,Manufacturer,Description,UnitPrice,ReorderLevel,SupplierId,IsVatExempt,RxRequired")] Medicine medicine)
        {
            if (id != medicine.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medicine);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Medicine updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Medicines.Any(e => e.Id == medicine.Id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Suppliers = _context.Suppliers.ToList();
            return View(medicine);
        }

        [Authorize(Roles = "Admin,InventoryCoordinator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var medicine = await _context.Medicines.Include(m => m.Supplier).FirstOrDefaultAsync(m => m.Id == id);
            if (medicine == null) return NotFound();
            return View(medicine);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,InventoryCoordinator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine != null)
            {
                _context.Medicines.Remove(medicine);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Medicine archived successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ---- Batch management (adds stock directly, outside a formal PO) ----
        [HttpGet]
        [Authorize(Roles = "Admin,InventoryCoordinator")]
        public async Task<IActionResult> AddBatch(int medicineId)
        {
            var medicine = await _context.Medicines.FindAsync(medicineId);
            if (medicine == null) return NotFound();
            ViewBag.Medicine = medicine;
            return View(new MedicineBatch { MedicineId = medicineId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,InventoryCoordinator")]
        public async Task<IActionResult> AddBatch([Bind("MedicineId,BatchNumber,Quantity,ExpiryDate,DateReceived")] MedicineBatch batch)
        {
            if (ModelState.IsValid)
            {
                _context.MedicineBatches.Add(batch);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Stock batch added.";
                return RedirectToAction(nameof(Details), new { id = batch.MedicineId });
            }
            ViewBag.Medicine = await _context.Medicines.FindAsync(batch.MedicineId);
            return View(batch);
        }
    }
}
