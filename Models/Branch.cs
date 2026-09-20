using System.ComponentModel.DataAnnotations;

namespace CarePlusPharmacy.Models
{
    public class Branch
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        [Display(Name = "Branch Name")]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(120)]
        [Display(Name = "Opening Hours")]
        public string? OpeningHours { get; set; }

        [Display(Name = "Main Branch")]
        public bool IsMainBranch { get; set; } = false;
    }
}
