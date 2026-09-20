using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlusPharmacy.Models
{
    // A refill plan the pharmacy offers (e.g. "Losartan 50mg Monthly Refill").
    // Admin defines these; Customers subscribe to them.
    public class SubscriptionPlan
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Plan Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Medicine")]
        public int MedicineId { get; set; }
        public Medicine? Medicine { get; set; }

        [Required, Range(1, int.MaxValue)]
        [Display(Name = "Quantity per Refill")]
        public int Quantity { get; set; } = 1;

        [Required, Range(1, 365)]
        [Display(Name = "Refill Interval (days)")]
        public int IntervalDays { get; set; } = 30;

        [Required, Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Price per Refill (₱)")]
        public decimal Price { get; set; }

        [Display(Name = "Active / Available")]
        public bool IsActive { get; set; } = true;

        public ICollection<CustomerSubscription> Subscriptions { get; set; } = new List<CustomerSubscription>();
    }
}
