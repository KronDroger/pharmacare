using CarePlusPharmacy.Data;
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

        public async Task<IActionResult> Index()
        {
            var billings = await _context.Billings
                .Include(b => b.Sale).ThenInclude(s => s!.Customer)
                .Include(b => b.Sale).ThenInclude(s => s!.Details).ThenInclude(d => d.Medicine)
                .OrderByDescending(b => b.DateIssued)
                .ToListAsync();
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
