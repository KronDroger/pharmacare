using System.ComponentModel.DataAnnotations;

namespace CarePlusPharmacy.Models
{
    public enum SubscriptionStatus { Active, Paused, Cancelled }

    // A customer's enrollment in a SubscriptionPlan (auto-refill).
    public class CustomerSubscription
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        [Display(Name = "Subscription Plan")]
        public int SubscriptionPlanId { get; set; }
        public SubscriptionPlan? SubscriptionPlan { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Next Refill Date")]
        public DateTime NextRefillDate { get; set; } = DateTime.Today;

        [Display(Name = "Status")]
        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        [StringLength(30)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";

        public ICollection<Sale> RefillSales { get; set; } = new List<Sale>();
    }
}
