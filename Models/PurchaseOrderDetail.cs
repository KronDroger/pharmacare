using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlusPharmacy.Models
{
    public class PurchaseOrderDetail
    {
        public int Id { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }

        [Required]
        [Display(Name = "Medicine")]
        public int MedicineId { get; set; }
        public Medicine? Medicine { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required, Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Unit Cost (₱)")]
        public decimal UnitCost { get; set; }
    }
}
