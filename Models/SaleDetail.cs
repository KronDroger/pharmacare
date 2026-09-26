using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlusPharmacy.Models
{
    public class SaleDetail
    {
        public int Id { get; set; }

        [Required]
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        [Required]
        [Display(Name = "Medicine")]
        public int MedicineId { get; set; }
        public Medicine? Medicine { get; set; }

        [Display(Name = "Dispensed Batch")]
        public int? BatchId { get; set; }
        public MedicineBatch? Batch { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required, Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Unit Price (₱)")]
        public decimal UnitPrice { get; set; }

        // "Buy 1 Take 1" saving on this line, in pesos. Non-zero only when the line
        // was dispensed from a near-expiry batch (MedicineBatch.IsNearExpiry), where
        // every 2 units taken from that batch are charged for only 1.
        // Quantity and UnitPrice always hold the true dispensed value at full price,
        // so inventory and audit remain truthful; this column records the giveaway.
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "BOGO Discount (₱)")]
        public decimal BogoDiscountAmount { get; set; } = 0m;

        // Full dispensed value of the line (what the goods are worth at list price).
        [NotMapped]
        public decimal LineTotal => Quantity * UnitPrice;

        // What the customer is actually charged for this line after BOGO.
        [NotMapped]
        public decimal ChargedTotal => LineTotal - BogoDiscountAmount;
    }
}
