using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Customer Management / CRM module.
    // Admin + Cashier: full CRUD (front-desk registration at point of sale).
    // Pharmacist: read-only (dashed line in Use Case Diagram — Customer CRM view).
    [Authorize(Roles = "Admin,Cashier,Pharmacist")]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CustomersController(ApplicationDbContext context) => _context = context;

        private bool CanEdit => User.IsInRole("Admin") || User.IsInRole("Cashier");

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Customers.Include(c => c.Sales).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.FullName.Contains(search));
            ViewBag.Search = search;
            ViewBag.CanEdit = CanEdit;
            return View(await query.OrderBy(c => c.FullName).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var customer = await _context.Customers
                .Include(c => c.Sales).ThenInclude(s => s.Details).ThenInclude(d => d.Medicine)
                .Include(c => c.Prescriptions)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [Authorize(Roles = "Admin,Cashier")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> Create([Bind("FullName,Phone,Email,Address")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(customer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Customer added successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Phone,Email,Address,DateRegistered")] Customer customer)
        {
            if (id != customer.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(customer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Customer updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Cashier")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Customer deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
