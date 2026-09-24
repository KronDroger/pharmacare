using System.ComponentModel.DataAnnotations;

namespace CarePlusPharmacy.Models
{
    public enum MembershipStatus { Active, Paused, Cancelled }

    // A customer's enrollment in a paid MembershipTier (e.g. "Health Plus VIP").
    public class CustomerMembership
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        [Display(Name = "Membership Tier")]
        public int MembershipTierId { get; set; }
        public MembershipTier? MembershipTier { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Next Billing Date")]
        public DateTime NextBillingDate { get; set; } = DateTime.Today;

        [Display(Name = "Status")]
        public MembershipStatus Status { get; set; } = MembershipStatus.Active;

        [StringLength(30)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";
    }
}