using System.ComponentModel.DataAnnotations;

namespace CarePlusPharmacy.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        [Display(Name = "Supplier Name")]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(30)]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; } = string.Empty;

        [EmailAddress, StringLength(120)]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public ICollection<Medicine> Medicines { get; set; } = new List<Medicine>();
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }
}
