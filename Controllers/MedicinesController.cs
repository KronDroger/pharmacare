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

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Medicines.Include(m => m.Supplier).Include(m => m.Batches).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.Name.Contains(search) || m.Category.Contains(search));

            ViewBag.Search = search;
            ViewBag.CanEdit = CanEdit;
            return View(await query.OrderBy(m => m.Name).ToListAsync());
        }

        // Expiry Monitoring — tracks batch-level expiration dates and financial risk
        public async Task<IActionResult> Expiring()
        {
            var batches = await _context.MedicineBatches
                .Include(b => b.Medicine)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();
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
        public async Task<IActionResult> Create([Bind("Name,GenericName,Category,Manufacturer,Description,UnitPrice,ReorderLevel,SupplierId")] Medicine medicine)
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,GenericName,Category,Manufacturer,Description,UnitPrice,ReorderLevel,SupplierId")] Medicine medicine)
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
