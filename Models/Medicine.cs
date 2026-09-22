using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlusPharmacy.Models
{
    public class Medicine
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Medicine / Brand Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        [Display(Name = "Generic / Chemical Name")]
        public string? GenericName { get; set; }

        [Required, StringLength(80)]
        public string Category { get; set; } = string.Empty;

        [StringLength(120)]
        [Display(Name = "Manufacturer / Brand Lab")]
        public string? Manufacturer { get; set; }

        [StringLength(400)]
        public string? Description { get; set; }

        [Required, Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Unit Price (₱)")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Reorder Level")]
        public int ReorderLevel { get; set; } = 50;

        // VAT-Exempt medicines (medicinal products covered by RA 10963 VAT exemptions /
        // certain prescription maintenance medicines) are not charged 12% VAT at the counter.
        [Display(Name = "VAT-Exempt Medicine")]
        public bool IsVatExempt { get; set; } = false;

        [Required]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public ICollection<MedicineBatch> Batches { get; set; } = new List<MedicineBatch>();

        [NotMapped]
        public int TotalStock => Batches?.Sum(b => b.Quantity) ?? 0;

        // Units that are actually sellable today: non-expired batches with remaining quantity.
        // Expired stock is quarantined for write-off and must never reach a customer.
        [NotMapped]
        public int SellableStock => Batches?
            .Where(b => b.Quantity > 0 && b.ExpiryDate >= DateTime.Today)
            .Sum(b => b.Quantity) ?? 0;

        [NotMapped]
        public int ExpiredStock => Batches?
            .Where(b => b.Quantity > 0 && b.ExpiryDate < DateTime.Today)
            .Sum(b => b.Quantity) ?? 0;

        [NotMapped]
        public DateTime? NearestExpiry => Batches != null && Batches.Any()
            ? Batches.Min(b => b.ExpiryDate)
            : null;

        [NotMapped]
        public DateTime? NearestSellableExpiry
        {
            get
            {
                var sellable = Batches?
                    .Where(b => b.Quantity > 0 && b.ExpiryDate >= DateTime.Today)
                    .ToList();
                return sellable != null && sellable.Any() ? sellable.Min(b => b.ExpiryDate) : null;
            }
        }
    }
}
