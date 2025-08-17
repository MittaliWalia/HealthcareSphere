namespace HealthSphere_CapstoneProject.Models
{
    public class Appointment
    {


        public int? Id { get; set; }
        public int? PatientId { get; set; }
        public int? DoctorId { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public TimeSpan? AppointmentTime { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }

        
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
    }
}
