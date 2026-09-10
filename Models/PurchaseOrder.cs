using System.ComponentModel.DataAnnotations;

namespace CarePlusPharmacy.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;

        public ICollection<PurchaseOrderDetail> Details { get; set; } = new List<PurchaseOrderDetail>();

        public decimal TotalCost => Details?.Sum(d => d.Quantity * d.UnitCost) ?? 0;
    }
}
