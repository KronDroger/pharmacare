using System.ComponentModel.DataAnnotations;

namespace CarePlusPharmacy.Models.ViewModels
{
    // Simple single-line POS entry form for the "temporary simple CRUD" flow.
    // Extend with a line-item collection later for a full shopping-cart style POS.
    public class SaleCreateViewModel
    {
        [Display(Name = "Customer (optional — leave blank for walk-in)")]
        public int? CustomerId { get; set; }

        [Required]
        [Display(Name = "Medicine")]
        public int MedicineId { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }
}
