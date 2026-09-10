using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlusPharmacy.Models
{
    public class Billing
    {
        public int Id { get; set; }

        [Required]
        public int SaleId { get; set; }
        public Sale? Sale { get; set; }

        [Required, StringLength(30)]
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required, Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Amount Due (₱)")]
        public decimal AmountDue { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Amount Paid (₱)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Change (₱)")]
        public decimal ChangeAmount { get; set; } = 0;

        [StringLength(30)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        [DataType(DataType.Date)]
        [Display(Name = "Date Issued")]
        public DateTime DateIssued { get; set; } = DateTime.Today;
    }
}
