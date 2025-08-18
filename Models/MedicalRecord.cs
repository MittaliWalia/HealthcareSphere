namespace HealthSphere_CapstoneProject.Models
{
    public class MedicalRecord
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime DateCreated { get; set; }
        public string Diagnosis { get; set; }
        public string Prescription { get; set; }
        public string Vitals { get; set; }
    }

}
