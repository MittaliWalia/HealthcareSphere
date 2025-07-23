using HealthSphere_CapstoneProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Rotativa;

namespace HealthSphere_CapstoneProject.Controllers
{
    public class StaffController : Controller
    {

        private readonly string cc;


        public StaffController(IConfiguration configuration)
        {
            cc = configuration.GetConnectionString("MyConnectionString");
        }

        private int GetLoggedInUserId()
        {
            return Convert.ToInt32(HttpContext.Session.GetString("UserId"));
        }

        public IActionResult Schedule()
            {
            List<dynamic> appointments = new List<dynamic>();

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
SELECT A.Id, A.AppointmentDate, A.AppointmentTime, A.Status, A.Notes,
       U.Email AS PatientName
FROM Appointments A
JOIN Users U ON A.PatientId = U.UserID
WHERE A.DoctorId = @DoctorId
ORDER BY A.AppointmentDate, A.AppointmentTime";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@DoctorId", GetLoggedInUserId());
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
                        Notes = rdr["Notes"].ToString(),
                        PatientName = rdr["PatientName"].ToString()
                    });
                }
            }

            ViewBag.Appointments = appointments;
         
            return View();
            }

            [HttpPost]
            public IActionResult MarkComplete(int appointmentId)
            {
                using (SqlConnection con = new SqlConnection(cc))
                {
                    string query = "UPDATE Appointments SET Status = 'Completed' WHERE Id = @Id";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Id", appointmentId);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                return RedirectToAction("Schedule");
            }

            public IActionResult PatientRecords(int patientId)
            {
               
            List<dynamic> records = new List<dynamic>();

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
SELECT * FROM MedicalRecords
WHERE PatientId = @PatientId
ORDER BY DateCreated DESC";

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
                        Vitals = rdr["Vitals"].ToString()
                    });
                }
            }

            ViewBag.PatientId = patientId;
            ViewBag.DoctorId = GetLoggedInUserId(); // from session
            return View(records);
        }

          

            [HttpPost]
        public IActionResult UpdateRecord(MedicalRecord model)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
INSERT INTO MedicalRecords (PatientId, DoctorId, DateCreated, Diagnosis, Prescription, Vitals)
VALUES (@PatientId, @DoctorId, GETDATE(), @Diagnosis, @Prescription, @Vitals)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PatientId", model.PatientId);
                cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);
                cmd.Parameters.AddWithValue("@Diagnosis", model.Diagnosis);
                cmd.Parameters.AddWithValue("@Prescription", model.Prescription);
                cmd.Parameters.AddWithValue("@Vitals", model.Vitals ?? "");
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("PatientRecords", new { patientId = model.PatientId });
        }

        public IActionResult Diagnostics(int appointmentId)
        {
            dynamic appointment = null;

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
SELECT A.Id AS AppointmentId, A.AppointmentDate, A.AppointmentTime, A.Status,
       U.Email
AS PatientName
FROM Appointments A
JOIN Users U ON A.PatientId = U.UserID
WHERE A.Id = @Id";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", appointmentId);
                con.Open();
                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    appointment = new
                    {
                        AppointmentId = rdr["AppointmentId"],
                        AppointmentDate = Convert.ToDateTime(rdr["AppointmentDate"]),
                        AppointmentTime = TimeSpan.Parse(rdr["AppointmentTime"].ToString()),
                        Status = rdr["Status"].ToString(),
                        PatientName = rdr["PatientName"].ToString(),
                        DoctorId = GetLoggedInUserId()
                    };
                }
            }

            return View(appointment);
        }


        [HttpPost]
        public IActionResult SaveDiagnostics(ClinicalNote model)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
INSERT INTO ClinicalNotes (AppointmentId, DoctorId, Notes, TreatmentPlan, LabTestRequested)
VALUES (@AppointmentId, @DoctorId, @Notes, @TreatmentPlan, @LabTestRequested)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);
                cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);
                cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                cmd.Parameters.AddWithValue("@TreatmentPlan", model.TreatmentPlan ?? "");
                cmd.Parameters.AddWithValue("@LabTestRequested", model.LabTestRequested);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Schedule");
        }


        [HttpPost]
        public IActionResult RescheduleAppointment(int appointmentId, DateTime newDate, TimeSpan newTime)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
UPDATE Appointments 
SET AppointmentDate = @Date, AppointmentTime = @Time, Status = 'Rescheduled'
WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Date", newDate.Date);
                cmd.Parameters.AddWithValue("@Time", newTime);
                cmd.Parameters.AddWithValue("@Id", appointmentId);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Schedule");
        }
        public IActionResult LabRequests()
        {
            List<dynamic> labTests = new List<dynamic>();

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
SELECT C.AppointmentId, U.Email AS PatientName, C.LabTestRequested, C.Notes
FROM ClinicalNotes C
JOIN Appointments A ON C.AppointmentId = A.Id
JOIN Users U ON A.PatientId = U.UserID
WHERE C.LabTestRequested = 1 AND C.DoctorId = @DoctorId";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@DoctorId", GetLoggedInUserId());
                con.Open();

                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    labTests.Add(new
                    {
                        AppointmentId = rdr["AppointmentId"],
                        PatientName = rdr["PatientName"].ToString(),
                        Notes = rdr["Notes"].ToString()
                    });
                }
            }

            ViewBag.LabTests = labTests;
            return View();
        }
        public IActionResult AssignNurse(int appointmentId)
        {
            dynamic appointment = null;
            List<dynamic> nurses = new List<dynamic>();

            using (SqlConnection con = new SqlConnection(cc))
            {
                con.Open();

                // Get appointment + patient
                var cmd1 = new SqlCommand(@"
SELECT A.Id AS AppointmentId, A.AppointmentDate, A.AppointmentTime, A.Status,
       U.Name AS PatientName
FROM Appointments A
JOIN Users U ON A.PatientId = U.UserID
WHERE A.Id = @Id", con);
                cmd1.Parameters.AddWithValue("@Id", appointmentId);
                var rdr1 = cmd1.ExecuteReader();
                if (rdr1.Read())
                {
                    appointment = new
                    {
                        AppointmentId = appointmentId,
                        AppointmentDate = Convert.ToDateTime(rdr1["AppointmentDate"]),
                        AppointmentTime = TimeSpan.Parse(rdr1["AppointmentTime"].ToString()),
                        Status = rdr1["Status"].ToString(),
                        PatientName = rdr1["PatientName"].ToString()
                    };
                }
                rdr1.Close();

                // Get list of nurses
                var cmd2 = new SqlCommand("SELECT UserID, Name FROM Users WHERE Role = 'Nurse'", con);
                var rdr2 = cmd2.ExecuteReader();
                while (rdr2.Read())
                {
                    nurses.Add(new
                    {
                        UserID = rdr2["UserID"],
                        Name = rdr2["Name"].ToString()
                    });
                }
            }

            appointment.Nurses = nurses;
            return View(appointment);
        }

        [HttpPost]
        public IActionResult AssignNurse(int appointmentId, int nurseId)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = "UPDATE Appointments SET NurseId = @NurseId WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@NurseId", nurseId);
                cmd.Parameters.AddWithValue("@Id", appointmentId);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Schedule");
        }
        public IActionResult Dashboard()
        {
            var doctorId = GetLoggedInUserId();
            int todayCount = 0, totalPatients = 0, labCount = 0;

            using (SqlConnection con = new SqlConnection(cc))
            {
                con.Open();

                var cmd1 = new SqlCommand("SELECT COUNT(*) FROM Appointments WHERE DoctorId = @Id AND AppointmentDate = CAST(GETDATE() AS DATE)", con);
                cmd1.Parameters.AddWithValue("@Id", doctorId);
                todayCount = (int)cmd1.ExecuteScalar();

                var cmd2 = new SqlCommand("SELECT COUNT(DISTINCT PatientId) FROM Appointments WHERE DoctorId = @Id", con);
                cmd2.Parameters.AddWithValue("@Id", doctorId);
                totalPatients = (int)cmd2.ExecuteScalar();

                var cmd3 = new SqlCommand("SELECT COUNT(*) FROM ClinicalNotes WHERE DoctorId = @Id AND LabTestRequested = 1", con);
                cmd3.Parameters.AddWithValue("@Id", doctorId);
                labCount = (int)cmd3.ExecuteScalar();
            }

            ViewBag.TodayAppointments = todayCount;
            ViewBag.TotalPatients = totalPatients;
            ViewBag.LabCount = labCount;

            return View();
        }
        public IActionResult DownloadPatientRecord(int patientId)
        {
            var records = GetMedicalRecords(patientId);
            return new Rotativa.AspNetCore.ViewAsPdf("PatientRecordsPdf", records)
            {
                FileName = $"Patient_{patientId}_Records.pdf"
            };
        }

        private List<MedicalRecord> GetMedicalRecords(int patientId)
        {
            List<MedicalRecord> records = new List<MedicalRecord>();

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
SELECT Id, PatientId, DoctorId, DateCreated, Diagnosis, Prescription, Vitals
FROM MedicalRecords
WHERE PatientId = @PatientId
ORDER BY DateCreated DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PatientId", patientId);
                con.Open();

                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    records.Add(new MedicalRecord
                    {
                        Id = Convert.ToInt32(rdr["Id"]),
                        PatientId = Convert.ToInt32(rdr["PatientId"]),
                        DoctorId = Convert.ToInt32(rdr["DoctorId"]),
                        DateCreated = Convert.ToDateTime(rdr["DateCreated"]),
                        Diagnosis = rdr["Diagnosis"].ToString(),
                        Prescription = rdr["Prescription"].ToString(),
                        Vitals = rdr["Vitals"].ToString()
                    });
                }
            }

            return records;
        }


        [HttpPost]
        public IActionResult UpdateInsurance(int BillId, string InsuranceProvider, string InsuranceNumber)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
UPDATE Bills
SET InsuranceProvider = @Provider,
    InsuranceNumber = @Number
WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Provider", InsuranceProvider);
                cmd.Parameters.AddWithValue("@Number", InsuranceNumber);
                cmd.Parameters.AddWithValue("@Id", BillId);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Billing");
        }
        public IActionResult DownloadInvoice(int billId)
        {
            dynamic bill = null;

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
SELECT B.Id, B.Amount, B.Status, B.InsuranceProvider, B.InsuranceNumber,
       B.DateIssued, B.Notes,
       A.AppointmentDate, A.AppointmentTime,
       D.Name AS DoctorName, P.Name AS PatientName
FROM Bills B
JOIN Appointments A ON B.AppointmentId = A.Id
JOIN Users D ON A.DoctorId = D.UserID
JOIN Users P ON B.PatientId = P.UserID
WHERE B.Id = @Id";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", billId);
                con.Open();

                var rdr = cmd.ExecuteReader();
                if (rdr.Read())
                {
                    bill = new
                    {
                        BillId = rdr["Id"],
                        Amount = Convert.ToDecimal(rdr["Amount"]),
                        Status = rdr["Status"].ToString(),
                        InsuranceProvider = rdr["InsuranceProvider"].ToString(),
                        InsuranceNumber = rdr["InsuranceNumber"].ToString(),
                        DateIssued = Convert.ToDateTime(rdr["DateIssued"]),
                        Notes = rdr["Notes"].ToString(),
                        AppointmentDate = Convert.ToDateTime(rdr["AppointmentDate"]),
                        AppointmentTime = TimeSpan.Parse(rdr["AppointmentTime"].ToString()),
                        DoctorName = rdr["DoctorName"].ToString(),
                        PatientName = rdr["PatientName"].ToString()
                    };
                }
            }

            return new Rotativa.AspNetCore.ViewAsPdf("InvoicePdf", bill)
            {
                FileName = $"Invoice_{billId}.pdf"
            };
        }
        public IActionResult DownloadMedicalHistory()
        {
            int patientId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));
            List<dynamic> records = new List<dynamic>();
            string patientName = "";

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
SELECT M.DateCreated, M.Diagnosis, M.Prescription, M.Vitals, U.Name AS DoctorName, P.Name AS PatientName
FROM MedicalRecords M
JOIN Users U ON M.DoctorId = U.UserID
JOIN Users P ON M.PatientId = P.UserID
WHERE M.PatientId = @PatientId
ORDER BY M.DateCreated DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PatientId", patientId);
                con.Open();

                var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    if (string.IsNullOrEmpty(patientName))
                        patientName = rdr["PatientName"].ToString();

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

            var model = new
            {
                PatientName = patientName,
                Records = records
            };

            return new Rotativa.AspNetCore.ViewAsPdf("MedicalHistoryPdf", model)
            {
                FileName = $"Medical_History_{patientId}.pdf"
            };
        }

    }
}

