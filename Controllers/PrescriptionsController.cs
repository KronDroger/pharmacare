using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Prescription Records module — Admin + Pharmacist only (transaction use case).
    [Authorize(Roles = "Admin,Pharmacist")]
    public class PrescriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PrescriptionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, string? status, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
        {
            var baseQuery = _context.Prescriptions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                baseQuery = baseQuery.Where(p =>
                    (p.Customer != null && p.Customer.FullName.Contains(search))
                    || p.DoctorName.Contains(search)
                    || p.Details.Any(d => d.Medicine != null && d.Medicine.Name.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PrescriptionStatus>(status, true, out var prescriptionStatus))
            {
                baseQuery = baseQuery.Where(p => p.Status == prescriptionStatus);
            }

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                baseQuery = baseQuery.Where(p => p.DatePrescribed >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date.AddDays(1);
                baseQuery = baseQuery.Where(p => p.DatePrescribed < toDate);
            }

            ViewBag.TotalCountAll = await baseQuery.CountAsync();
            ViewBag.PendingCountAll = await baseQuery.CountAsync(p => p.Status == PrescriptionStatus.Pending);
            ViewBag.FulfilledCountAll = await baseQuery.CountAsync(p => p.Status == PrescriptionStatus.Fulfilled);

            ViewBag.Filters = new List<Models.ViewModels.FilterField>
            {
                new() { Name = "search", Label = "Search patient, doctor, or medicine", Type = Models.ViewModels.FilterFieldType.Text, Value = search },
                new() { Name = "status", Label = "Fulfillment Status", Type = Models.ViewModels.FilterFieldType.Select, Value = status,
                    Options = new List<Models.ViewModels.FilterOption>
                    {
                        new() { Value = "Pending", Label = "Pending" },
                        new() { Value = "Fulfilled", Label = "Fulfilled" },
                        new() { Value = "Cancelled", Label = "Cancelled" }
                    } },
                new() { Name = "from", Label = "From", Type = Models.ViewModels.FilterFieldType.Date, Value = from?.ToString("yyyy-MM-dd") },
                new() { Name = "to", Label = "To", Type = Models.ViewModels.FilterFieldType.Date, Value = to?.ToString("yyyy-MM-dd") }
            };

            var query = baseQuery
                .Include(p => p.Customer)
                .Include(p => p.Details).ThenInclude(d => d.Medicine)
                .OrderByDescending(p => p.DatePrescribed)
                .ThenByDescending(p => p.Id);

            var prescriptions = await PaginatedList<Prescription>.CreateAsync(query, page, pageSize);
            return View(prescriptions);
        }

        public IActionResult Create()
        {
            ViewBag.Customers = _context.Customers.ToList();
            ViewBag.Medicines = _context.Medicines.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int customerId, string doctorName, DateTime datePrescribed, int medicineId, int quantity, string dosage)
        {
            if (customerId == 0 || string.IsNullOrWhiteSpace(doctorName) || medicineId == 0 || quantity <= 0)
            {
                ModelState.AddModelError(string.Empty, "Please complete all required fields.");
                ViewBag.Customers = _context.Customers.ToList();
                ViewBag.Medicines = _context.Medicines.ToList();
                return View();
            }

            var prescription = new Prescription
            {
                CustomerId = customerId,
                DoctorName = doctorName,
                DatePrescribed = datePrescribed == default ? DateTime.Today : datePrescribed,
                Status = PrescriptionStatus.Pending,
                Details = new List<PrescriptionDetail>
                {
                    new() { MedicineId = medicineId, Quantity = quantity, Dosage = dosage ?? string.Empty }
                }
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();
            await LogAuditAsync("PRESCRIPTION_CREATED", $"Recorded prescription #{prescription.Id} for {prescription.DoctorName} (medicine ID {medicineId}, qty {quantity}).");
            TempData["Success"] = "Prescription successfully recorded.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Fulfill(int id)
        {
            var rx = await _context.Prescriptions.FindAsync(id);
            if (rx == null) return NotFound();

            if (rx.Status != PrescriptionStatus.Pending)
            {
                TempData["Error"] = $"Only pending prescriptions can be fulfilled. This prescription is {rx.Status}.";
                return RedirectToAction(nameof(Index));
            }

            rx.Status = PrescriptionStatus.Fulfilled;
            await _context.SaveChangesAsync();
            await LogAuditAsync("PRESCRIPTION_FULFILLED", $"Fulfilled prescription #{rx.Id} (patient ID {rx.CustomerId}).");
            TempData["Success"] = "Prescription marked as fulfilled.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reopen(int id)
        {
            var rx = await _context.Prescriptions.FindAsync(id);
            if (rx == null) return NotFound();

            // Fulfilled → Pending requires an Admin override (pharmacists cannot
            // silently flip an already-dispensed prescription back).
            if (!User.IsInRole("Admin"))
            {
                TempData["Error"] = "Only an Admin can reopen a fulfilled prescription.";
                return RedirectToAction(nameof(Index));
            }

            if (rx.Status == PrescriptionStatus.Cancelled)
            {
                TempData["Error"] = "Cancelled prescriptions cannot be reopened.";
                return RedirectToAction(nameof(Index));
            }

            if (rx.Status != PrescriptionStatus.Fulfilled)
            {
                TempData["Error"] = "Only fulfilled prescriptions can be reopened.";
                return RedirectToAction(nameof(Index));
            }

            rx.Status = PrescriptionStatus.Pending;
            await _context.SaveChangesAsync();
            await LogAuditAsync("PRESCRIPTION_REOPENED", $"Admin override: reopened prescription #{rx.Id} (was Fulfilled, now Pending).");
            TempData["Success"] = "Prescription reopened (Admin override logged).";
            return RedirectToAction(nameof(Index));
        }

        // Replaces the old hard delete — prescriptions are clinical records and are
        // never removed; a mistaken entry is cancelled with a documented reason.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string cancelReason)
        {
            var rx = await _context.Prescriptions.FindAsync(id);
            if (rx == null) return NotFound();

            if (rx.Status == PrescriptionStatus.Fulfilled)
            {
                TempData["Error"] = $"Prescription #{rx.Id} was already dispensed. It cannot be cancelled — void or refund the sale instead.";
                return RedirectToAction(nameof(Index));
            }

            if (rx.Status == PrescriptionStatus.Cancelled)
            {
                TempData["Error"] = "This prescription is already cancelled.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(cancelReason) || cancelReason.Length > 500)
            {
                TempData["Error"] = "A cancellation reason is required (max 500 characters).";
                return RedirectToAction(nameof(Index));
            }

            rx.Status = PrescriptionStatus.Cancelled;
            await _context.SaveChangesAsync();
            await LogAuditAsync("PRESCRIPTION_CANCELLED", $"Cancelled prescription #{rx.Id}: {cancelReason.Trim()}");
            TempData["Success"] = "Prescription cancelled and logged.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LogAuditAsync(string action, string details)
        {
            var user = await _userManager.GetUserAsync(User);
            _context.AuditLogs.Add(new AuditLog
            {
                Timestamp = DateTime.Now,
                UserId = user?.Id,
                UserName = user?.FullName ?? user?.UserName ?? "Staff",
                UserRole = User.IsInRole("Admin") ? "Admin" : "Pharmacist",
                Action = action,
                Module = "Prescriptions",
                Details = details,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            });
            await _context.SaveChangesAsync();
        }
    }
}