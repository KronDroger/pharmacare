using System.ComponentModel.DataAnnotations;

namespace CarePlusPharmacy.Models
{
    // Audit Trail for regulatory compliance, pharmacy security, and system tracking
    public class AuditLog
    {
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UserId { get; set; }

        [Required, StringLength(120)]
        [Display(Name = "User / Staff")]
        public string UserName { get; set; } = "System";

        [StringLength(50)]
        [Display(Name = "Role")]
        public string UserRole { get; set; } = "System";

        [Required, StringLength(80)]
        [Display(Name = "Action")]
        public string Action { get; set; } = string.Empty;

        [Required, StringLength(80)]
        [Display(Name = "Module")]
        public string Module { get; set; } = string.Empty;

        [Required, StringLength(500)]
        [Display(Name = "Details / Reference")]
        public string Details { get; set; } = string.Empty;

        [StringLength(50)]
        public string? IpAddress { get; set; }
    }
}
