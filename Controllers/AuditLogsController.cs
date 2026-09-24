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

        public async Task<IActionResult> Index(string? module, string? search, string? role, DateTime? from, DateTime? to, int page = 1, int pageSize = 10, bool print = false)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(module))
                query = query.Where(a => a.Module == module);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(a => a.UserName.Contains(search) || a.Details.Contains(search) || a.Action.Contains(search) || a.Module.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(a => a.UserRole == role);

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                query = query.Where(a => a.Timestamp >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date.AddDays(1);
                query = query.Where(a => a.Timestamp < toDate);
            }

            ViewBag.Module = module;
            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.Modules = await _context.AuditLogs.Select(a => a.Module).Distinct().ToListAsync();
            var roles = await _context.AuditLogs.Select(a => a.UserRole).Distinct().Where(r => r != null).OrderBy(r => r).ToListAsync();

            ViewBag.Filters = new List<Models.ViewModels.FilterField>
            {
                new() { Name = "search", Label = "Search log text", Type = Models.ViewModels.FilterFieldType.Text, Value = search },
                new() { Name = "module", Label = "Module", Type = Models.ViewModels.FilterFieldType.Select, Value = module,
                    Options = ((List<string>)ViewBag.Modules).Select(m => new Models.ViewModels.FilterOption { Value = m, Label = m }).ToList() },
                new() { Name = "role", Label = "Role", Type = Models.ViewModels.FilterFieldType.Select, Value = role,
                    Options = roles.Select(r => new Models.ViewModels.FilterOption { Value = r!, Label = r! }).ToList() },
                new() { Name = "from", Label = "From", Type = Models.ViewModels.FilterFieldType.Date, Value = from?.ToString("yyyy-MM-dd") },
                new() { Name = "to", Label = "To", Type = Models.ViewModels.FilterFieldType.Date, Value = to?.ToString("yyyy-MM-dd") }
            };

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
