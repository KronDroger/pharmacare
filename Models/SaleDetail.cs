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

        [NotMapped]
        public decimal LineTotal => Quantity * UnitPrice;
    }
}
