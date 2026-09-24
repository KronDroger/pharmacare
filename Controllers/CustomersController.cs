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

        public async Task<IActionResult> Index(string? search, string? city, string? gender, string? tier, int page = 1, int pageSize = 10)
        {
            var query = _context.Customers.Include(c => c.Sales).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(c =>
                    c.FullName.Contains(search)
                    || c.Phone.Contains(search)
                    || (c.Email != null && c.Email.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(c => c.City == city);
            }

            if (!string.IsNullOrWhiteSpace(gender))
            {
                query = query.Where(c => c.Gender == gender);
            }

            if (!string.IsNullOrWhiteSpace(tier))
            {
                switch (tier)
                {
                    case "Platinum": query = query.Where(c => c.LoyaltyPoints >= 500); break;
                    case "Gold": query = query.Where(c => c.LoyaltyPoints >= 250 && c.LoyaltyPoints < 500); break;
                    case "Silver": query = query.Where(c => c.LoyaltyPoints >= 100 && c.LoyaltyPoints < 250); break;
                    default: query = query.Where(c => c.LoyaltyPoints < 100); break;
                }
            }

            ViewBag.Search = search;
            ViewBag.CanEdit = CanEdit;

            var cities = await _context.Customers.Where(c => c.City != null).Select(c => c.City!).Distinct().OrderBy(c => c).ToListAsync();
            ViewBag.Filters = new List<Models.ViewModels.FilterField>
            {
                new() { Name = "search", Label = "Search name, phone, or email", Type = Models.ViewModels.FilterFieldType.Text, Value = search },
                new() { Name = "city", Label = "City", Type = Models.ViewModels.FilterFieldType.Select, Value = city,
                    Options = cities.Select(c => new Models.ViewModels.FilterOption { Value = c, Label = c }).ToList() },
                new() { Name = "gender", Label = "Gender", Type = Models.ViewModels.FilterFieldType.Select, Value = gender,
                    Options = new List<Models.ViewModels.FilterOption>
                    {
                        new() { Value = "Male", Label = "Male" },
                        new() { Value = "Female", Label = "Female" }
                    } },
                new() { Name = "tier", Label = "CRM Tier", Type = Models.ViewModels.FilterFieldType.Select, Value = tier,
                    Options = new List<Models.ViewModels.FilterOption>
                    {
                        new() { Value = "Bronze", Label = "Bronze Member" },
                        new() { Value = "Silver", Label = "Silver Member" },
                        new() { Value = "Gold", Label = "Gold Member" },
                        new() { Value = "Platinum", Label = "Platinum VIP" }
                    } }
            };

            var customers = await PaginatedList<Customer>.CreateAsync(query.OrderBy(c => c.FullName), page, pageSize);
            return View(customers);
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
        public async Task<IActionResult> Create([Bind("FullName,Phone,Email,Address,City,DateOfBirth,Gender")] Customer customer)
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Phone,Email,Address,City,DateOfBirth,Gender")] Customer customer)
        {
            if (id != customer.Id) return NotFound();

            if (!ModelState.IsValid) return View(customer);

            // Load the persisted entity and copy only the editable contact/demographic
            // fields. LoyaltyPoints and DateRegistered are system-managed and must
            // never be overwritten by an edit form (previously _context.Update()
            // silently wiped them because they were not part of the Bind list).
            var existing = await _context.Customers.FindAsync(id);
            if (existing == null) return NotFound();

            existing.FullName = customer.FullName;
            existing.Phone = customer.Phone;
            existing.Email = customer.Email;
            existing.Address = customer.Address;
            existing.City = customer.City;
            existing.DateOfBirth = customer.DateOfBirth;
            existing.Gender = customer.Gender;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Customer updated successfully.";
            return RedirectToAction(nameof(Index));
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
