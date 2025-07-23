namespace HealthSphere_CapstoneProject.Models
{
    public class Bill
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public string? InsuranceProvider { get; set; }
        public string? InsuranceNumber { get; set; }
        public DateTime DateIssued { get; set; }
        public string? Notes { get; set; }

        // Extra
        public string? PatientName { get; set; }
        public DateTime AppointmentDate { get; set; }
    }
}
