using System.ComponentModel.DataAnnotations;

namespace CarePlusPharmacy.Models
{
    public class Prescription
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Required, StringLength(120)]
        [Display(Name = "Prescribing Doctor")]
        public string DoctorName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date Prescribed")]
        public DateTime DatePrescribed { get; set; } = DateTime.Today;

        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Pending;

        // Set when the prescription is dispensed from the POS — links the
        // fulfilled prescription to the Sale it was fulfilled through.
        [Display(Name = "Fulfilling Sale")]
        public int? SaleId { get; set; }
        public Sale? Sale { get; set; }

        public ICollection<PrescriptionDetail> Details { get; set; } = new List<PrescriptionDetail>();
    }
}
