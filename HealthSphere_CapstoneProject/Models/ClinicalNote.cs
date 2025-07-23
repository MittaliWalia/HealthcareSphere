namespace HealthSphere_CapstoneProject.Models
{
    public class ClinicalNote
    {
        public int AppointmentId { get; set; }
        public int DoctorId { get; set; }
        public string Notes { get; set; }
        public string TreatmentPlan { get; set; }
        public bool LabTestRequested { get; set; }
    }
}
