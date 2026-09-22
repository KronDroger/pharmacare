using System.ComponentModel.DataAnnotations;

namespace CarePlusPharmacy.Models.ViewModels
{
    // Receiving form for a Purchase Order: inventory staff assigns a real
    // batch number and expiry date to every ordered line before stock is added.
    public class PurchaseOrderReceiveViewModel
    {
        public int PurchaseOrderId { get; set; }

        [Display(Name = "Purchase Order #")]
        public int OrderNumber => PurchaseOrderId;

        [Display(Name = "Supplier")]
        public string SupplierName { get; set; } = string.Empty;

        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; }

        public List<PurchaseOrderReceiveLine> Lines { get; set; } = new();
    }

    public class PurchaseOrderReceiveLine
    {
        public int DetailId { get; set; }
        public int MedicineId { get; set; }

        [Display(Name = "Medicine")]
        public string MedicineName { get; set; } = string.Empty;

        [Display(Name = "Generic Name")]
        public string? GenericName { get; set; }

        [Display(Name = "Ordered Quantity")]
        public int Quantity { get; set; }

        [Display(Name = "Unit Cost (₱)")]
        public decimal UnitCost { get; set; }

        [Required(ErrorMessage = "Batch number is required for receiving.")]
        [StringLength(40)]
        [Display(Name = "Batch Number")]
        public string BatchNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry date is required for receiving.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        public DateTime ExpiryDate { get; set; }
    }
}