using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlusPharmacy.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress, StringLength(120)]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(80)]
        public string? City { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(30)]
        public string? Gender { get; set; }

        [Display(Name = "Loyalty Points")]
        public int LoyaltyPoints { get; set; } = 0;

        [DataType(DataType.Date)]
        [Display(Name = "Date Registered")]
        public DateTime DateRegistered { get; set; } = DateTime.Today;

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();
        public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public ICollection<CustomerMembership> Memberships { get; set; } = new List<CustomerMembership>();

        [Display(Name = "CRM Tier")]
        public string Tier => LoyaltyPoints switch
        {
            >= 500 => "Platinum VIP",
            >= 250 => "Gold Member",
            >= 100 => "Silver Member",
            _ => "Bronze Member"
        };

        // The currently enrolled paid membership (most recent Active record), or
        // null when the customer has none. Not persisted - computed from Memberships.
        [NotMapped]
        public CustomerMembership? CurrentMembership
            => Memberships?
                .Where(m => m.Status == MembershipStatus.Active)
                .OrderByDescending(m => m.StartDate)
                .FirstOrDefault();
    }
}
