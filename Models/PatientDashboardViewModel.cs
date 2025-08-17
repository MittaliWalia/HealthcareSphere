namespace HealthSphere_CapstoneProject.Models
{
    public class PatientDashboardViewModel
    {
        public Patient Patient { get; set; }
        public List<Appointment> UpcomingAppointments { get; set; }
        public List<Bill> OutstandingBills { get; set; }
    }
}
