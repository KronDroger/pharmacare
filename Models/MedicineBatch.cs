using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlusPharmacy.Models
{
    // Supports Expiry Monitoring: each purchased batch is tracked separately
    // because the same medicine can arrive with different expiry dates.
    public class MedicineBatch
    {
        // Batches expiring within this many days are flagged as near-expiry and
        // become eligible for the automatic "Buy 1 Take 1" promo instead of being written off.
        public const int NearExpiryThresholdDays = 120;

        public int Id { get; set; }

        [Required]
        public int MedicineId { get; set; }
        public Medicine? Medicine { get; set; }

        [Required, StringLength(40)]
        [Display(Name = "Batch Number")]
        public string BatchNumber { get; set; } = string.Empty;

        [Required, Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Required, DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date Received")]
        public DateTime DateReceived { get; set; } = DateTime.Today;

        // Near-expiry = still sellable today, but expiring within the threshold window.
        // Drives BOGO eligibility so near-expiring stock is discounted out of inventory
        // rather than written off.
        [NotMapped]
        public bool IsNearExpiry => Quantity > 0
            && ExpiryDate.Date > DateTime.Today
            && ExpiryDate.Date <= DateTime.Today.AddDays(NearExpiryThresholdDays);
    }
}
