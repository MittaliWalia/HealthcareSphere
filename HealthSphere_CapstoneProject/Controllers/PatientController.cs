using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace HealthSphere_CapstoneProject.Controllers
{
    public class PatientController : Controller
    {


        private readonly string cc;


        public PatientController(IConfiguration configuration)
        {
            cc = configuration.GetConnectionString("MyConnectionString");
        }

        public IActionResult Dashboard()
        {
            int patientId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));
            List<dynamic> appointments = new List<dynamic>();

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
SELECT A.Id, A.AppointmentDate, A.AppointmentTime, A.Status,
       U.Email AS DoctorName
FROM Appointments A
JOIN Users U ON A.DoctorId = U.UserID
WHERE A.PatientId = @PatientId
AND A.AppointmentDate >= CAST(GETDATE() AS DATE)
ORDER BY A.AppointmentDate, A.AppointmentTime";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PatientId", patientId);
                con.Open();

                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    appointments.Add(new
                    {
                        Id = rdr["Id"],
                        AppointmentDate = Convert.ToDateTime(rdr["AppointmentDate"]),
                        AppointmentTime = TimeSpan.Parse(rdr["AppointmentTime"].ToString()),
                        Status = rdr["Status"].ToString(),
                        DoctorName = rdr["DoctorName"].ToString()
                    });
                }
            }

            ViewBag.Appointments = appointments;
            return View();
        }


        public IActionResult MyRecords()
        {
            int patientId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));
            List<dynamic> records = new List<dynamic>();

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
SELECT M.DateCreated, M.Diagnosis, M.Prescription, M.Vitals, U.Name AS DoctorName
FROM MedicalRecords M
JOIN Users U ON M.DoctorId = U.UserID
WHERE M.PatientId = @PatientId
ORDER BY M.DateCreated DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PatientId", patientId);
                con.Open();

                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    records.Add(new
                    {
                        DateCreated = Convert.ToDateTime(rdr["DateCreated"]),
                        Diagnosis = rdr["Diagnosis"].ToString(),
                        Prescription = rdr["Prescription"].ToString(),
                        Vitals = rdr["Vitals"].ToString(),
                        DoctorName = rdr["DoctorName"].ToString()
                    });
                }
            }

            ViewBag.Records = records;
            return View();
        }


        public IActionResult Billing()
        {
            int patientId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));
            List<dynamic> bills = new List<dynamic>();

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
SELECT B.Id, B.Amount, B.Status, B.InsuranceProvider, B.InsuranceNumber, B.DateIssued, A.AppointmentDate
FROM Bills B
JOIN Appointments A ON B.AppointmentId = A.Id
WHERE B.PatientId = @PatientId
ORDER BY B.DateIssued DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PatientId", patientId);
                con.Open();

                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    bills.Add(new
                    {
                        Id = rdr["Id"],
                        Amount = Convert.ToDecimal(rdr["Amount"]),
                        Status = rdr["Status"].ToString(),
                        InsuranceProvider = rdr["InsuranceProvider"].ToString(),
                        InsuranceNumber = rdr["InsuranceNumber"].ToString(),
                        DateIssued = Convert.ToDateTime(rdr["DateIssued"]),
                        AppointmentDate = Convert.ToDateTime(rdr["AppointmentDate"])
                    });
                }
            }

            ViewBag.Bills = bills;
            return View();
        }
        [HttpPost]
        public IActionResult MarkAsPaid(int billId)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = "UPDATE Bills SET Status = 'Paid' WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", billId);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Billing");
        }
    }
}
