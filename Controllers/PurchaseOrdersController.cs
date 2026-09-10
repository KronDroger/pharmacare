using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Purchasing module — Admin + Inventory Coordinator only.
    [Authorize(Roles = "Admin,InventoryCoordinator")]
    public class PurchaseOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PurchaseOrdersController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var orders = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Details).ThenInclude(d => d.Medicine)
                .OrderByDescending(p => p.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        public IActionResult Create()
        {
            ViewBag.Suppliers = _context.Suppliers.ToList();
            ViewBag.Medicines = _context.Medicines.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int supplierId, int medicineId, int quantity, decimal unitCost)
        {
            if (quantity <= 0 || unitCost < 0 || supplierId == 0 || medicineId == 0)
            {
                ModelState.AddModelError(string.Empty, "Please provide a valid supplier, medicine, quantity and cost.");
                ViewBag.Suppliers = _context.Suppliers.ToList();
                ViewBag.Medicines = _context.Medicines.ToList();
                return View();
            }

            var order = new PurchaseOrder
            {
                SupplierId = supplierId,
                OrderDate = DateTime.Today,
                Status = PurchaseOrderStatus.Pending,
                Details = new List<PurchaseOrderDetail>
                {
                    new() { MedicineId = medicineId, Quantity = quantity, UnitCost = unitCost }
                }
            };

            _context.PurchaseOrders.Add(order);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Purchase order created.";
            return RedirectToAction(nameof(Index));
        }

        // Receiving a PO adds the ordered quantity as a new medicine batch (stock-in).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReceived(int id)
        {
            var order = await _context.PurchaseOrders.Include(p => p.Details).FirstOrDefaultAsync(p => p.Id == id);
            if (order == null) return NotFound();

            if (order.Status != PurchaseOrderStatus.Received)
            {
                foreach (var item in order.Details)
                {
                    _context.MedicineBatches.Add(new MedicineBatch
                    {
                        MedicineId = item.MedicineId,
                        BatchNumber = $"PO-{order.Id}-{item.MedicineId}",
                        Quantity = item.Quantity,
                        ExpiryDate = DateTime.Today.AddYears(2),
                        DateReceived = DateTime.Today
                    });
                }
                order.Status = PurchaseOrderStatus.Received;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Purchase order received and stock updated.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.PurchaseOrders.FindAsync(id);
            if (order != null && order.Status == PurchaseOrderStatus.Pending)
            {
                order.Status = PurchaseOrderStatus.Cancelled;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.PurchaseOrders.FindAsync(id);
            if (order != null)
            {
                _context.PurchaseOrders.Remove(order);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
