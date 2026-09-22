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

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var baseQuery = _context.Billings.AsQueryable();
            var kpiQuery = baseQuery.Where(b => b.Sale == null || !b.Sale!.IsVoided);

            ViewBag.TotalBilledAll = await kpiQuery.SumAsync(b => b.AmountDue);
            ViewBag.TotalPaidAll = await kpiQuery.Where(b => b.PaymentStatus == PaymentStatus.Paid).SumAsync(b => b.AmountDue);
            ViewBag.TotalCountAll = await kpiQuery.CountAsync();

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
