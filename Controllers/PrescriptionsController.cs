using CarePlusPharmacy.Data;
using CarePlusPharmacy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Controllers
{
    // Prescription Records module — Admin + Pharmacist only (transaction use case).
    [Authorize(Roles = "Admin,Pharmacist")]
    public class PrescriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public PrescriptionsController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Customer)
                .Include(p => p.Details).ThenInclude(d => d.Medicine)
                .OrderByDescending(p => p.DatePrescribed)
                .ToListAsync();
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
            TempData["Success"] = "Prescription successfully recorded.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var rx = await _context.Prescriptions.FindAsync(id);
            if (rx != null)
            {
                rx.Status = rx.Status == PrescriptionStatus.Fulfilled
                    ? PrescriptionStatus.Pending
                    : PrescriptionStatus.Fulfilled;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rx = await _context.Prescriptions.FindAsync(id);
            if (rx != null)
            {
                _context.Prescriptions.Remove(rx);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
