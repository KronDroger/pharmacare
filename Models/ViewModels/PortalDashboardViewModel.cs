namespace CarePlusPharmacy.Models.ViewModels
{
    // Data shown on the Customer portal dashboard (Portal/Index).
    public class PortalDashboardViewModel
    {
        public Customer? Customer { get; set; }

        public int ActiveSubscriptionCount { get; set; }
        public int PausedSubscriptionCount { get; set; }
        public int PurchaseCount { get; set; }
        public int PendingPrescriptionCount { get; set; }

        public List<Sale> RecentPurchases { get; set; } = new();
        public List<CustomerSubscription> ActiveSubscriptions { get; set; } = new();
    }

    // Data shown on the Customer rewards page (Portal/Rewards).
    public class PortalRewardsViewModel
    {
        public Customer? Customer { get; set; }
        public List<Sale> Transactions { get; set; } = new();
    }

    // Data shown on the Customer membership page (Portal/Membership).
    public class PortalMembershipViewModel
    {
        public Customer? Customer { get; set; }
        public CustomerMembership? CurrentMembership { get; set; }
        public List<MembershipTier> Tiers { get; set; } = new();
        public List<CustomerMembership> History { get; set; } = new();
    }
}