using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Sales & POS module — Admin, Cashier, and Pharmacist access.
    [Authorize(Roles = "Admin,Cashier,Pharmacist")]
    public class SalesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SalesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // List all sales transactions
        public async Task<IActionResult> Index()
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Cashier)
                .Include(s => s.Billing)
                .Include(s => s.Details).ThenInclude(d => d.Medicine)
                .OrderByDescending(s => s.Id)
                .ToListAsync();
            return View(sales);
        }

        // Full-screen / Interactive POS Terminal
        public async Task<IActionResult> Create(int? prescriptionId)
        {
            ViewBag.Customers = await _context.Customers.OrderBy(c => c.FullName).ToListAsync();
            ViewBag.Medicines = await _context.Medicines
                .Include(m => m.Batches)
                .OrderBy(m => m.Name)
                .ToListAsync();

            // If an Rx is selected for dispensing
            if (prescriptionId.HasValue)
            {
                var rx = await _context.Prescriptions
                    .Include(p => p.Customer)
                    .Include(p => p.Details).ThenInclude(d => d.Medicine)
                    .FirstOrDefaultAsync(p => p.Id == prescriptionId.Value);
                ViewBag.Prescription = rx;
            }

            return View();
        }

        public class PosItemDto
        {
            public int MedicineId { get; set; }
            public int Quantity { get; set; }
        }

        public class PosCheckoutModel
        {
            public int? CustomerId { get; set; }
            public int? PrescriptionId { get; set; }
            public string PaymentMethod { get; set; } = "Cash";
            public decimal CashTendered { get; set; }
            public int PointsToRedeem { get; set; }
            public List<PosItemDto> Items { get; set; } = new();
        }

        // POS Checkout Endpoint
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromBody] PosCheckoutModel model)
        {
            if (model == null || model.Items == null || !model.Items.Any())
            {
                return BadRequest(new { success = false, message = "Cart is empty. Please select medicines to dispense." });
            }

            var saleDetails = new List<SaleDetail>();
            decimal grossTotal = 0;

            // Validate inventory and deduct batches FIFO
            foreach (var item in model.Items)
            {
                if (item.Quantity <= 0) continue;

                var medicine = await _context.Medicines
                    .Include(m => m.Batches)
                    .FirstOrDefaultAsync(m => m.Id == item.MedicineId);

                if (medicine == null)
                {
                    return BadRequest(new { success = false, message = $"Medicine ID {item.MedicineId} not found." });
                }

                if (medicine.TotalStock < item.Quantity)
                {
                    return BadRequest(new { success = false, message = $"Insufficient stock for {medicine.Name}. Only {medicine.TotalStock} units available." });
                }

                // Deduct stock oldest-batch-first (FIFO)
                int remainingToDeduct = item.Quantity;
                var activeBatches = medicine.Batches
                    .Where(b => b.Quantity > 0)
                    .OrderBy(b => b.ExpiryDate)
                    .ToList();

                foreach (var batch in activeBatches)
                {
                    if (remainingToDeduct <= 0) break;
                    int take = Math.Min(batch.Quantity, remainingToDeduct);
                    batch.Quantity -= take;
                    remainingToDeduct -= take;

                    saleDetails.Add(new SaleDetail
                    {
                        MedicineId = medicine.Id,
                        BatchId = batch.Id,
                        Quantity = take,
                        UnitPrice = medicine.UnitPrice
                    });

                    grossTotal += take * medicine.UnitPrice;
                }
            }

            // Customer & Loyalty Points processing
            Customer? customer = null;
            decimal discountAmount = 0;
            int pointsEarned = 0;

            if (model.CustomerId.HasValue && model.CustomerId.Value > 0)
            {
                customer = await _context.Customers.FindAsync(model.CustomerId.Value);
                if (customer != null)
                {
                    // 1 point = ₱1.00 discount (capped at grossTotal and customer's available points)
                    if (model.PointsToRedeem > 0)
                    {
                        int maxRedeemable = Math.Min(customer.LoyaltyPoints, (int)grossTotal);
                        int actualRedeem = Math.Min(model.PointsToRedeem, maxRedeemable);
                        discountAmount = actualRedeem;
                        customer.LoyaltyPoints -= actualRedeem;
                    }

                    // Earn 1 point per ₱100 spent on net amount
                    decimal netPayable = Math.Max(0, grossTotal - discountAmount);
                    pointsEarned = (int)(netPayable / 100);
                    customer.LoyaltyPoints += pointsEarned;
                }
            }

            decimal finalAmount = Math.Max(0, grossTotal - discountAmount);
            decimal cashTendered = model.CashTendered >= finalAmount ? model.CashTendered : finalAmount;
            decimal changeAmount = Math.Max(0, cashTendered - finalAmount);

            var sale = new Sale
            {
                CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
                CashierId = _userManager.GetUserId(User),
                PaymentMethod = string.IsNullOrWhiteSpace(model.PaymentMethod) ? "Cash" : model.PaymentMethod,
                SaleDate = DateTime.Now,
                DiscountAmount = discountAmount,
                PointsEarned = pointsEarned,
                PointsRedeemed = (int)discountAmount,
                Details = saleDetails
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            // Auto-generate official Billing Invoice
            var billing = new Billing
            {
                SaleId = sale.Id,
                InvoiceNumber = $"INV-{DateTime.Today.Year}-{sale.Id:D5}",
                AmountDue = finalAmount,
                AmountPaid = cashTendered,
                ChangeAmount = changeAmount,
                PaymentMethod = sale.PaymentMethod,
                PaymentStatus = PaymentStatus.Paid,
                DateIssued = DateTime.Now
            };
            _context.Billings.Add(billing);

            // If attached to a prescription, mark it fulfilled
            if (model.PrescriptionId.HasValue && model.PrescriptionId.Value > 0)
            {
                var rx = await _context.Prescriptions.FindAsync(model.PrescriptionId.Value);
                if (rx != null)
                {
                    rx.Status = PrescriptionStatus.Fulfilled;
                }
            }

            // Record compliance audit log
            var user = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                Timestamp = DateTime.Now,
                UserId = user?.Id,
                UserName = user?.FullName ?? user?.UserName ?? "Staff",
                UserRole = User.IsInRole("Admin") ? "Admin" : (User.IsInRole("Pharmacist") ? "Pharmacist" : "Cashier"),
                Action = "POS_SALE_COMPLETED",
                Module = "Sales & POS",
                Details = $"Processed Sale #{sale.Id} ({sale.PaymentMethod}): ₱{finalAmount:N2}, Issued {billing.InvoiceNumber}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                saleId = sale.Id,
                invoiceNumber = billing.InvoiceNumber,
                totalAmount = finalAmount,
                changeAmount = changeAmount,
                pointsEarned = pointsEarned,
                redirectUrl = Url.Action("Details", new { id = sale.Id })
            });
        }

        // Sale / Receipt Details (Printable official pharmacy layout)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Cashier)
                .Include(s => s.Details).ThenInclude(d => d.Medicine)
                .Include(s => s.Details).ThenInclude(d => d.Batch)
                .Include(s => s.Billing)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null) return NotFound();
            return View(sale);
        }
    }
}
