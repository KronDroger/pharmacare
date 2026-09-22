using System.Data;
using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using CarePlusPharmacy.Services;
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
        private readonly PricingService _pricingService;

        public SalesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, PricingService pricingService)
        {
            _context = context;
            _userManager = userManager;
            _pricingService = pricingService;
        }

        // List all sales transactions
        public async Task<IActionResult> Index(string? search, string? paymentMethod, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
        {
            var baseQuery = _context.Sales.AsQueryable();

            // Filters (Sales ledger search + payment method + date range)
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                baseQuery = baseQuery.Where(s =>
                    (s.Customer != null && s.Customer.FullName.Contains(search))
                    || s.Id.ToString().Contains(search)
                    || (s.Billing != null && s.Billing.InvoiceNumber != null && s.Billing.InvoiceNumber.Contains(search)));
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

            // KPIs reflect the filtered ledger (independent of page number)
            var allSalesData = await baseQuery.Select(s => new { Discount = s.DiscountAmount, Gross = s.Details.Sum(d => (decimal?)(d.Quantity * d.UnitPrice)) ?? 0m }).ToListAsync();
            ViewBag.TotalRevenueAll = allSalesData.Sum(s => Math.Max(0, s.Gross - s.Discount));
            ViewBag.TotalCountAll = await baseQuery.CountAsync();
            ViewBag.TotalItemsAll = await baseQuery.SelectMany(s => s.Details).SumAsync(d => (int?)d.Quantity) ?? 0;
            ViewBag.TotalPointsAll = await baseQuery.SumAsync(s => (int?)s.PointsEarned) ?? 0;

            ViewBag.Search = search;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

            var query = baseQuery
                .Include(s => s.Customer)
                .Include(s => s.Cashier)
                .Include(s => s.Billing)
                .Include(s => s.Details).ThenInclude(d => d.Medicine)
                .OrderByDescending(s => s.SaleDate)
                .ThenByDescending(s => s.Id);

            var sales = await PaginatedList<Sale>.CreateAsync(query, page, pageSize);
            return View(sales);
        }

        // Full-screen / Interactive POS Terminal
        public async Task<IActionResult> Create(int? prescriptionId)
        {
            ViewBag.Customers = await _context.Customers.OrderBy(c => c.FullName).ToListAsync();
            ViewBag.Categories = await _context.Medicines
                .Select(m => m.Category)
                .Distinct()
                .OrderBy(c => c)
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

        // Paged medicine catalog for the POS terminal (search-as-you-type + paged grid)
        [HttpGet]
        public async Task<IActionResult> Medicines(int page = 1, int pageSize = 12, string? search = null, string? category = null)
        {
            var query = _context.Medicines
                .Include(m => m.Batches)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(m => m.Name.Contains(search)
                    || (m.GenericName != null && m.GenericName.Contains(search))
                    || (m.Manufacturer != null && m.Manufacturer.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(m => m.Category == category);
            }

            query = query.OrderBy(m => m.Name);

            int totalCount = await query.CountAsync();
            int totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages));

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = items.Select(m => new
            {
                id = m.Id,
                name = m.Name,
                genericName = m.GenericName,
                category = m.Category,
                manufacturer = m.Manufacturer,
                unitPrice = m.UnitPrice,
                reorderLevel = m.ReorderLevel,
                totalStock = m.TotalStock,
                sellableStock = m.SellableStock,
                expiredStock = m.ExpiredStock,
                nearestExpiry = m.NearestExpiry?.ToString("yyyy-MM-dd"),
                isVatExempt = m.IsVatExempt
            }).ToList();

            return Json(new
            {
                success = true,
                data,
                page,
                pageSize,
                totalCount,
                totalPages
            });
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
            public SaleDiscountType DiscountType { get; set; } = SaleDiscountType.None;
            public string? DiscountIdNumber { get; set; }
            public List<PosItemDto> Items { get; set; } = new();
        }

        // POS Checkout Endpoint
        // Safe checkout: the server is authoritative for prices, sellable stock,
        // and FIFO batch deduction. The whole operation runs inside a single
        // transaction so stock can never be oversold by concurrent registers.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout([FromBody] PosCheckoutModel model)
        {
            if (model == null || model.Items == null || !model.Items.Any())
            {
                return BadRequest(new { success = false, message = "Cart is empty. Please select medicines to dispense." });
            }

            decimal finalAmount = 0;
            decimal discountAmount = 0;
            decimal cashTendered = 0;
            decimal changeAmount = 0;
            int pointsEarned = 0;
            int pointsRedeemed = 0;

            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                // Lock all medicines up-front in a stable order to make concurrent checkouts deadlock-free.
                var requestedItems = model.Items
                    .Where(i => i.Quantity > 0)
                    .OrderBy(i => i.MedicineId)
                    .ToList();

                var medicineIds = requestedItems.Select(i => i.MedicineId).ToList();
                var medicines = await _context.Medicines
                    .Include(m => m.Batches)
                    .Where(m => medicineIds.Contains(m.Id))
                    .OrderBy(m => m.Id)
                    .ToListAsync();

                var saleDetails = new List<SaleDetail>();
                decimal grossTotal = 0;

                foreach (var item in requestedItems)
                {
                    var medicine = medicines.FirstOrDefault(m => m.Id == item.MedicineId);
                    if (medicine == null)
                    {
                        return BadRequest(new { success = false, message = $"Medicine ID {item.MedicineId} not found." });
                    }

                    if (medicine.SellableStock < item.Quantity)
                    {
                        await tx.RollbackAsync();
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Insufficient sellable stock for {medicine.Name}. Only {medicine.SellableStock} units available (expired stock is not sellable)."
                        });
                    }

                    // Deduct stock FIFO — expired batches are never dispensed.
                    int remainingToDeduct = item.Quantity;
                    var activeBatches = medicine.Batches
                        .Where(b => b.Quantity > 0 && b.ExpiryDate >= DateTime.Today)
                        .OrderBy(b => b.ExpiryDate)
                        .ThenBy(b => b.Id)
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

                // Statutory Senior/PWD discount requires a valid government ID number.
                if (model.DiscountType == SaleDiscountType.Senior || model.DiscountType == SaleDiscountType.Pwd)
                {
                    if (string.IsNullOrWhiteSpace(model.DiscountIdNumber) || model.DiscountIdNumber.Length > 30)
                    {
                        await tx.RollbackAsync();
                        return BadRequest(new
                        {
                            success = false,
                            message = "Please provide the Senior Citizen / PWD ID number to apply the 20% discount."
                        });
                    }
                }

                // Price the sale (server-side VAT + statutory discount) — the client
                // never dictates totals.
                var priceLines = saleDetails.Select(d =>
                {
                    var med = medicines.FirstOrDefault(m => m.Id == d.MedicineId);
                    return new PriceLine(d.Quantity * d.UnitPrice, med?.IsVatExempt ?? false);
                });
                var price = _pricingService.Compute(priceLines, model.DiscountType);

                // Customer & Loyalty Points processing
                Customer? customer = null;

                if (model.CustomerId.HasValue && model.CustomerId.Value > 0)
                {
                    customer = await _context.Customers.FindAsync(model.CustomerId.Value);
                    if (customer != null)
                    {
                        // 1 point = ₱1.00 discount (capped at the remaining balance
                        // after the statutory discount, and at available points)
                        decimal payableBeforePoints = Math.Max(0, grossTotal - price.DiscountAmount);
                        if (model.PointsToRedeem > 0)
                        {
                            int maxRedeemable = Math.Min(customer.LoyaltyPoints, (int)payableBeforePoints);
                            int actualRedeem = Math.Min(model.PointsToRedeem, maxRedeemable);
                            discountAmount = actualRedeem;
                            customer.LoyaltyPoints -= actualRedeem;
                            pointsRedeemed = actualRedeem;
                        }

                        // Earn 1 point per ₱100 spent on the final net amount
                        decimal netPayable = Math.Max(0, grossTotal - price.DiscountAmount - discountAmount);
                        pointsEarned = (int)(netPayable / 100);
                        customer.LoyaltyPoints += pointsEarned;
                    }
                }

                finalAmount = Math.Max(0, grossTotal - price.DiscountAmount - discountAmount);
                cashTendered = model.CashTendered >= finalAmount ? model.CashTendered : finalAmount;
                changeAmount = Math.Max(0, cashTendered - finalAmount);

                var sale = new Sale
                {
                    CustomerId = model.CustomerId > 0 ? model.CustomerId : null,
                    CashierId = _userManager.GetUserId(User),
                    PaymentMethod = string.IsNullOrWhiteSpace(model.PaymentMethod) ? "Cash" : model.PaymentMethod,
                    SaleDate = DateTime.Now,
                    VatableSales = price.VatableSales,
                    VatExemptSales = price.VatExemptSales,
                    VatAmount = price.VatAmount,
                    DiscountType = model.DiscountType,
                    DiscountIdNumber = string.IsNullOrWhiteSpace(model.DiscountIdNumber) ? null : model.DiscountIdNumber.Trim(),
                    DiscountAmount = price.DiscountAmount + discountAmount,
                    PointsEarned = pointsEarned,
                    PointsRedeemed = pointsRedeemed,
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
                    Details = $"Processed Sale #{sale.Id} ({sale.PaymentMethod}): ₱{finalAmount:N2} gross ₱{grossTotal:N2} VAT ₱{sale.VatAmount:N2}{(sale.DiscountType != SaleDiscountType.None ? $", {sale.DiscountType} ID {sale.DiscountIdNumber}" : "")}, Issued {billing.InvoiceNumber}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    success = true,
                    saleId = sale.Id,
                    invoiceNumber = billing.InvoiceNumber,
                    totalAmount = finalAmount,
                    discountAmount = discountAmount,
                    cashTendered = cashTendered,
                    changeAmount = changeAmount,
                    pointsEarned = pointsEarned,
                    pointsRedeemed = pointsRedeemed,
                    redirectUrl = Url.Action("Details", new { id = sale.Id })
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
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
