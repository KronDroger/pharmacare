using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Billing module — invoices are generated automatically from Sales.
    // Admin + Cashier can view/manage; read-only ledger.
    [Authorize(Roles = "Admin,Cashier")]
    public class BillingController : Controller
    {
        private readonly ApplicationDbContext _context;
        public BillingController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(string? search, string? paymentMethod, string? status, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
        {
            var baseQuery = _context.Billings.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                baseQuery = baseQuery.Where(b =>
                    b.InvoiceNumber.Contains(search)
                    || b.SaleId.ToString().Contains(search)
                    || (b.Sale != null && b.Sale.Customer != null && b.Sale.Customer.FullName.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                baseQuery = baseQuery.Where(b => b.PaymentMethod == paymentMethod);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var paymentStatus))
            {
                baseQuery = baseQuery.Where(b => b.PaymentStatus == paymentStatus);
            }

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                baseQuery = baseQuery.Where(b => b.DateIssued >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date.AddDays(1);
                baseQuery = baseQuery.Where(b => b.DateIssued < toDate);
            }

            var kpiQuery = baseQuery.Where(b => b.Sale == null || !b.Sale!.IsVoided);

            ViewBag.TotalBilledAll = await kpiQuery.SumAsync(b => b.AmountDue);
            ViewBag.TotalPaidAll = await kpiQuery.Where(b => b.PaymentStatus == PaymentStatus.Paid).SumAsync(b => b.AmountDue);
            ViewBag.TotalCountAll = await kpiQuery.CountAsync();

            ViewBag.Filters = new List<Models.ViewModels.FilterField>
            {
                new() { Name = "search", Label = "Search invoice #, sale #, or customer", Type = Models.ViewModels.FilterFieldType.Text, Value = search },
                new() { Name = "paymentMethod", Label = "Payment Method", Type = Models.ViewModels.FilterFieldType.Select, Value = paymentMethod,
                    Options = new List<Models.ViewModels.FilterOption>
                    {
                        new() { Value = "Cash", Label = "Cash" },
                        new() { Value = "GCash", Label = "GCash" },
                        new() { Value = "Card", Label = "Card" },
                        new() { Value = "PayMongo", Label = "PayMongo" }
                    } },
                new() { Name = "status", Label = "Payment Status", Type = Models.ViewModels.FilterFieldType.Select, Value = status,
                    Options = new List<Models.ViewModels.FilterOption>
                    {
                        new() { Value = "Paid", Label = "Paid" },
                        new() { Value = "Unpaid", Label = "Unpaid" },
                        new() { Value = "Refunded", Label = "Refunded" }
                    } },
                new() { Name = "from", Label = "From", Type = Models.ViewModels.FilterFieldType.Date, Value = from?.ToString("yyyy-MM-dd") },
                new() { Name = "to", Label = "To", Type = Models.ViewModels.FilterFieldType.Date, Value = to?.ToString("yyyy-MM-dd") }
            };

            var query = baseQuery
                .Include(b => b.Sale).ThenInclude(s => s!.Customer)
                .Include(b => b.Sale).ThenInclude(s => s!.Details).ThenInclude(d => d.Medicine)
                .OrderByDescending(b => b.DateIssued);

            var billings = await PaginatedList<Billing>.CreateAsync(query, page, pageSize);
            return View(billings);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var billing = await _context.Billings
                .Include(b => b.Sale).ThenInclude(s => s!.Customer)
                .Include(b => b.Sale).ThenInclude(s => s!.Details).ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (billing == null) return NotFound();
            return View(billing);
        }
    }
}
