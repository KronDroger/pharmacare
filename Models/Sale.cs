using System.ComponentModel.DataAnnotations;

namespace CarePlusPharmacy.Models
{
    public class Sale
    {
        public int Id { get; set; }

        // Nullable: supports walk-in customers with no profile
        [Display(Name = "Customer")]
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Display(Name = "Cashier")]
        public string? CashierId { get; set; }
        public ApplicationUser? Cashier { get; set; }

        [Required, StringLength(30)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";

        [DataType(DataType.Date)]
        [Display(Name = "Sale Date")]
        public DateTime SaleDate { get; set; } = DateTime.Today;

        [Display(Name = "Discount (₱)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Display(Name = "Points Earned")]
        public int PointsEarned { get; set; } = 0;

        [Display(Name = "Points Redeemed")]
        public int PointsRedeemed { get; set; } = 0;

        public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();

        [Display(Name = "Gross Total")]
        public decimal GrossAmount => Details?.Sum(d => d.Quantity * d.UnitPrice) ?? 0;

        [Display(Name = "Net Total")]
        public decimal TotalAmount => Math.Max(0, GrossAmount - DiscountAmount);

        public Billing? Billing { get; set; }
    }
}
