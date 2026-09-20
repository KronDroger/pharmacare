using CarePlusPharmacy.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Security & Compliance Audit Trails — Admin and Pharmacist access per Use Case Diagram
    [Authorize(Roles = "Admin,Pharmacist")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AuditLogsController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(string? module, string? search, int page = 1, int pageSize = 10, bool print = false)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(module))
                query = query.Where(a => a.Module == module);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a => a.UserName.Contains(search) || a.Details.Contains(search) || a.Action.Contains(search));

            ViewBag.Module = module;
            ViewBag.Search = search;
            ViewBag.Modules = await _context.AuditLogs.Select(a => a.Module).Distinct().ToListAsync();
            ViewBag.Print = print;

            if (print)
            {
                var allLogs = await query.OrderByDescending(a => a.Timestamp).ToListAsync();
                var paginatedList = new Models.PaginatedList<Models.AuditLog>(allLogs, allLogs.Count, 1, Math.Max(1, allLogs.Count));
                return View(paginatedList);
            }

            var logs = await Models.PaginatedList<Models.AuditLog>.CreateAsync(query.OrderByDescending(a => a.Timestamp), page, pageSize);

            return View(logs);
        }
    }
}
