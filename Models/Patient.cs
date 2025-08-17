using DocumentFormat.OpenXml.Spreadsheet;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HealthSphere_CapstoneProject.Models
{

    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        [ForeignKey(nameof(User))]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string Gender { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [Phone]
        public string Phone { get; set; }

        public virtual Login User { get; set; }

        public virtual ICollection<Appointment> Appointments { get; set; }
            = new List<Appointment>();

        public virtual ICollection<Bill> Bills { get; set; }
            = new List<Bill>();
    }
}
