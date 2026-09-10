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

        [Required]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public ICollection<MedicineBatch> Batches { get; set; } = new List<MedicineBatch>();

        [NotMapped]
        public int TotalStock => Batches?.Sum(b => b.Quantity) ?? 0;

        [NotMapped]
        public DateTime? NearestExpiry => Batches != null && Batches.Any()
            ? Batches.Min(b => b.ExpiryDate)
            : null;
    }
}
