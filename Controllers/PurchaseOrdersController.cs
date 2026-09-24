using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using CarePlusPharmacy.Models.ViewModels;
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

        public async Task<IActionResult> Index(string? search, string? status, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
        {
            var baseQuery = _context.PurchaseOrders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                baseQuery = baseQuery.Where(p =>
                    p.Id.ToString().Contains(search)
                    || (p.Supplier != null && p.Supplier.Name.Contains(search))
                    || p.Details.Any(d => d.Medicine != null && d.Medicine.Name.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PurchaseOrderStatus>(status, true, out var orderStatus))
            {
                baseQuery = baseQuery.Where(p => p.Status == orderStatus);
            }

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                baseQuery = baseQuery.Where(p => p.OrderDate >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date.AddDays(1);
                baseQuery = baseQuery.Where(p => p.OrderDate < toDate);
            }

            ViewBag.Filters = new List<Models.ViewModels.FilterField>
            {
                new() { Name = "search", Label = "Search supplier, medicine, or PO #", Type = Models.ViewModels.FilterFieldType.Text, Value = search },
                new() { Name = "status", Label = "Order Status", Type = Models.ViewModels.FilterFieldType.Select, Value = status,
                    Options = new List<Models.ViewModels.FilterOption>
                    {
                        new() { Value = "Pending", Label = "Pending" },
                        new() { Value = "Received", Label = "Received" },
                        new() { Value = "Cancelled", Label = "Cancelled" }
                    } },
                new() { Name = "from", Label = "From", Type = Models.ViewModels.FilterFieldType.Date, Value = from?.ToString("yyyy-MM-dd") },
                new() { Name = "to", Label = "To", Type = Models.ViewModels.FilterFieldType.Date, Value = to?.ToString("yyyy-MM-dd") }
            };

            var query = baseQuery
                .Include(p => p.Supplier)
                .Include(p => p.Details).ThenInclude(d => d.Medicine)
                .OrderByDescending(p => p.OrderDate)
                .ThenByDescending(p => p.Id);
            var orders = await PaginatedList<PurchaseOrder>.CreateAsync(query, page, pageSize);
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

        // Receiving form: staff confirms batch number + expiry per line item
        // before the ordered quantity is added to stock.
        [HttpGet]
        public async Task<IActionResult> Receive(int id)
        {
            var order = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Details).ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (order == null) return NotFound();

            if (order.Status != PurchaseOrderStatus.Pending)
            {
                TempData["Error"] = "Only pending purchase orders can be received.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new PurchaseOrderReceiveViewModel
            {
                PurchaseOrderId = order.Id,
                SupplierName = order.Supplier?.Name ?? "—",
                OrderDate = order.OrderDate,
                Lines = order.Details
                    .OrderBy(d => d.Id)
                    .Select(d => new PurchaseOrderReceiveLine
                    {
                        DetailId = d.Id,
                        MedicineId = d.MedicineId,
                        MedicineName = d.Medicine?.Name ?? $"Medicine #{d.MedicineId}",
                        GenericName = d.Medicine?.GenericName,
                        Quantity = d.Quantity,
                        UnitCost = d.UnitCost,
                        BatchNumber = $"PO-{order.Id}-{d.MedicineId}",
                        ExpiryDate = DateTime.Today.AddMonths(24)
                    })
                    .ToList()
            };

            return View(vm);
        }

        // Receiving a PO adds each ordered quantity as a real medicine batch (stock-in).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Receive(int id, PurchaseOrderReceiveViewModel model)
        {
            var order = await _context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.Details)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (order == null) return NotFound();

            if (order.Status != PurchaseOrderStatus.Pending)
            {
                TempData["Error"] = "Only pending purchase orders can be received.";
                return RedirectToAction(nameof(Index));
            }

            if (model.Lines == null || !model.Lines.Any())
            {
                model.PurchaseOrderId = order.Id;
                TempData["Error"] = "No line items to receive.";
                return RedirectToAction(nameof(Receive), new { id });
            }

            // Map submitted lines back to order details for validation.
            var detailMap = order.Details.ToDictionary(d => d.Id);
            var today = DateTime.Today;
            var submitErrors = new List<string>();

            foreach (var line in model.Lines)
            {
                if (!detailMap.TryGetValue(line.DetailId, out var detail))
                {
                    submitErrors.Add($"Unknown line item #{line.DetailId}.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line.BatchNumber))
                {
                    submitErrors.Add($"Batch number is required for {line.MedicineName ?? detail.Medicine?.Name ?? "item"}.");
                }

                if (line.ExpiryDate == default || line.ExpiryDate < today)
                {
                    submitErrors.Add($"Expiry date must be today or later for {line.MedicineName ?? detail.Medicine?.Name ?? "item"}.");
                }
            }

            if (submitErrors.Any())
            {
                model.PurchaseOrderId = order.Id;
                TempData["Error"] = string.Join(" ", submitErrors);
                return RedirectToAction(nameof(Receive), new { id });
            }

            foreach (var line in model.Lines)
            {
                if (!detailMap.TryGetValue(line.DetailId, out var detail)) continue;

                _context.MedicineBatches.Add(new MedicineBatch
                {
                    MedicineId = detail.MedicineId,
                    BatchNumber = line.BatchNumber.Trim(),
                    Quantity = detail.Quantity,
                    ExpiryDate = line.ExpiryDate,
                    DateReceived = today
                });
            }

            order.Status = PurchaseOrderStatus.Received;

            _context.AuditLogs.Add(new AuditLog
            {
                Timestamp = DateTime.Now,
                UserName = User.Identity?.Name ?? "Staff",
                UserRole = User.IsInRole("Admin") ? "Admin" : "InventoryCoordinator",
                Action = "PO_RECEIVED",
                Module = "Purchasing (PO)",
                Details = $"Received PO #{order.Id} from {order.Supplier?.Name} ({order.Details.Count} line item(s)) into stock."
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Purchase order #{order.Id} received — stock updated with real batch numbers and expiry dates.";
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
