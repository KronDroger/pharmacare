using System.ComponentModel.DataAnnotations;

namespace CarePlusPharmacy.Models
{
    public class PrescriptionDetail
    {
        public int Id { get; set; }

        [Required]
        public int PrescriptionId { get; set; }
        public Prescription? Prescription { get; set; }

        [Required]
        [Display(Name = "Medicine")]
        public int MedicineId { get; set; }
        public Medicine? Medicine { get; set; }

        [StringLength(100)]
        [Display(Name = "Dosage Instructions")]
        public string Dosage { get; set; } = string.Empty;

        [Required, Range(1, int.MaxValue)]
        [Display(Name = "Prescribed Quantity")]
        public int Quantity { get; set; }
    }
}
