using System.ComponentModel.DataAnnotations;

namespace CarePlusPharmacy.Models
{
    // Supports Expiry Monitoring: each purchased batch is tracked separately
    // because the same medicine can arrive with different expiry dates.
    public class MedicineBatch
    {
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
    }
}
