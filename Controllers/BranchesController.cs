using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    [Authorize]
    public class BranchesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BranchesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? type, int page = 1, int pageSize = 10)
        {
            var query = _context.Branches.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(b =>
                    b.Name.Contains(search)
                    || b.Address.Contains(search)
                    || b.Phone.Contains(search));
            }

            if (string.Equals(type, "Main", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => b.IsMainBranch);
            }
            else if (string.Equals(type, "Subsidiary", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => !b.IsMainBranch);
            }

            ViewBag.Filters = new List<Models.ViewModels.FilterField>
            {
                new() { Name = "search", Label = "Search name, address, or phone", Type = Models.ViewModels.FilterFieldType.Text, Value = search },
                new() { Name = "type", Label = "Branch Type", Type = Models.ViewModels.FilterFieldType.Select, Value = type,
                    Options = new List<Models.ViewModels.FilterOption>
                    {
                        new() { Value = "Main", Label = "Main Branch" },
                        new() { Value = "Subsidiary", Label = "Subsidiary" }
                    } }
            };

            var branches = await PaginatedList<Branch>.CreateAsync(
                query.OrderByDescending(b => b.IsMainBranch).ThenBy(b => b.Name),
                page,
                pageSize);
            return View(branches);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Name,Address,Phone,OpeningHours,IsMainBranch")] Branch branch)
        {
            if (ModelState.IsValid)
            {
                _context.Add(branch);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Branch added.";
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound();
            return View(branch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Address,Phone,OpeningHours,IsMainBranch")] Branch branch)
        {
            if (id != branch.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(branch);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Branch updated.";
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch != null)
            {
                _context.Branches.Remove(branch);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Branch removed.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
