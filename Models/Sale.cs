using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "Vatable Sales")]
        public decimal VatableSales { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "VAT-Exempt Sales")]
        public decimal VatExemptSales { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "VAT Amount (12%)")]
        public decimal VatAmount { get; set; } = 0;

        // Statutory discount type applied (None / Senior / Pwd). The monetary discount
        // itself is part of DiscountAmount below, alongside any points redemption.
        [Display(Name = "Discount Type")]
        public SaleDiscountType DiscountType { get; set; } = SaleDiscountType.None;

        [StringLength(30)]
        [Display(Name = "Discount ID Number")]
        public string? DiscountIdNumber { get; set; }

        [Display(Name = "Discount (₱)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Display(Name = "Points Earned")]
        public int PointsEarned { get; set; } = 0;

        [Display(Name = "Points Redeemed")]
        public int PointsRedeemed { get; set; } = 0;

        // Void / refund of a completed sale. Stock is returned to the original
        // batches, points are reversed, and the invoice is marked Refunded.
        [Display(Name = "Voided")]
        public bool IsVoided { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "Void Reason")]
        public string? VoidReason { get; set; }

        [Display(Name = "Voided By")]
        public string? VoidedById { get; set; }
        public ApplicationUser? VoidedBy { get; set; }

        [Display(Name = "Voided At")]
        public DateTime? VoidedAt { get; set; }

        public ICollection<SaleDetail> Details { get; set; } = new List<SaleDetail>();

        [Display(Name = "Gross Total")]
        public decimal GrossAmount => Details?.Sum(d => d.Quantity * d.UnitPrice) ?? 0;

        [Display(Name = "Net Total")]
        public decimal TotalAmount => Math.Max(0, GrossAmount - DiscountAmount);

        public Billing? Billing { get; set; }
    }
}
