using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlusPharmacy.Models
{
    // A paid membership tier customers subscribe to (discount %, free delivery,
    // premium perks). Distinct from SubscriptionPlan (per-medicine auto-refill);
    // both features coexist.
    public class MembershipTier
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Tier Name")]
        public string Name { get; set; } = string.Empty;

        [Required, Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Monthly Price (₱)")]
        public decimal MonthlyPrice { get; set; }

        [Required, Range(0, 100), Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Discount Percent")]
        public decimal DiscountPercent { get; set; }

        [Display(Name = "Free Delivery")]
        public bool FreeDelivery { get; set; }

        [Display(Name = "Max Family Accounts")]
        public int MaxFamilyAccounts { get; set; } = 1;

        [Display(Name = "Dedicated Pharmacist")]
        public bool HasDedicatedPharmacist { get; set; }

        [Display(Name = "Priority Dispensing")]
        public bool PriorityDispensing { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public ICollection<CustomerMembership> Memberships { get; set; } = new List<CustomerMembership>();
    }
}