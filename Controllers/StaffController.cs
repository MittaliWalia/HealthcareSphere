using HealthSphere_CapstoneProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Rotativa;
using System.Data;

namespace HealthSphere_CapstoneProject.Controllers
{
    public class StaffController : Controller
    {


        private readonly string cc;


        public StaffController(
            IConfiguration config
           )
        {
            cc = config.GetConnectionString("MyConnectionString");

        }



        public IActionResult Schedule()
        {
            var list = new List<Appointment>();
            const string sql = @"
        SELECT 
            a.Id,
            u.Username      AS PatientName,
            a.AppointmentDate,
            a.AppointmentTime,
            a.Status,
            a.Notes
        FROM Appointments a
        INNER JOIN Users u 
            ON a.PatientId = u.UserID
        WHERE a.DoctorId = @did
        ORDER BY a.AppointmentDate, a.AppointmentTime
    ";

            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand(sql, con))
            {
                var docId = Convert.ToInt32(HttpContext.Session.GetString("UserId"));

                cmd.Parameters.Add("@did", SqlDbType.Int).Value = docId;
                con.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        list.Add(new Appointment
                        {
                            Id = (int)dr["Id"],
                            PatientName = dr["PatientName"].ToString(),
                            AppointmentDate = (DateTime)dr["AppointmentDate"],
                            AppointmentTime = (TimeSpan)dr["AppointmentTime"],
                            Status = dr["Status"].ToString(),
                            Notes = dr["Notes"].ToString()
                        });
                    }
                }
            }


            return View(list);
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

        public IActionResult LabAndDiagnostics()
        {
            var doctorId = GetLoggedInUserId();
            if (doctorId == null) return RedirectToAction("Index", "Home");

            var labRequests = new List<dynamic>();
            var diagnostics = new List<dynamic>();

            using (var con = new SqlConnection(cc))
            {
                con.Open();

                var labCmd = new SqlCommand(@"
            SELECT C.AppointmentId, U.Email AS PatientName, C.Notes
            FROM ClinicalNotes C
            JOIN Appointments A ON C.AppointmentId = A.Id
            JOIN Users U ON A.PatientId = U.UserID
            WHERE C.LabTestRequested = 1 AND C.DoctorId = @did", con);
                labCmd.Parameters.AddWithValue("@did", doctorId);

                using (var r1 = labCmd.ExecuteReader())
                    while (r1.Read())
                        labRequests.Add(new
                        {
                            AppointmentId = r1["AppointmentId"],
                            PatientName = r1["PatientName"].ToString(),
                            Notes = r1["Notes"].ToString()
                        });


                var diagCmd = new SqlCommand(@"
            SELECT C.AppointmentId, U.Email AS PatientName, C.Notes, C.TreatmentPlan
            FROM ClinicalNotes C
            JOIN Appointments A ON C.AppointmentId = A.Id
            JOIN Users U ON A.PatientId = U.UserID
            WHERE C.DoctorId = @did", con);
                diagCmd.Parameters.AddWithValue("@did", doctorId);

                using (var r2 = diagCmd.ExecuteReader())
                    while (r2.Read())
                        diagnostics.Add(new
                        {
                            AppointmentId = r2["AppointmentId"],
                            PatientName = r2["PatientName"].ToString(),
                            Notes = r2["Notes"].ToString(),
                            TreatmentPlan = r2["TreatmentPlan"].ToString()
                        });
            }

            ViewBag.LabRequests = labRequests;
            ViewBag.Diagnostics = diagnostics;
            return View();
        }
        [HttpGet]
        public IActionResult CreateDiagnostic()
        {
            var doctorId = GetLoggedInUserId();
            List<dynamic> appointments = new();

            using (var con = new SqlConnection(cc))
            {
                string query = @"
        SELECT A.Id, U.Username AS PatientName, A.AppointmentDate, A.AppointmentTime
        FROM Appointments A
        JOIN Users U ON A.PatientId = U.UserID
        WHERE A.DoctorId = @Did
        ORDER BY A.AppointmentDate DESC";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Did", doctorId);
                con.Open();
                var dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    appointments.Add(new
                    {
                        Id = (int)dr["Id"],
                        Display = $"{dr["PatientName"]} - {Convert.ToDateTime(dr["AppointmentDate"]).ToShortDateString()} @ {TimeSpan.Parse(dr["AppointmentTime"].ToString()):hh\\:mm}"
                    });
                }
            }

            ViewBag.Appointments = appointments;
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDiagnostic(ClinicalNote model)
        {
            model.DoctorId ??= GetLoggedInUserId();
            if (model.DoctorId == null)
                return RedirectToAction("Dashboard");



            using (var con = new SqlConnection(cc))
            {
                var query = @"
        INSERT INTO ClinicalNotes (AppointmentId, DoctorId, Notes, TreatmentPlan, LabTestRequested)
        VALUES (@AppointmentId, @DoctorId, @Notes, @Plan, @Lab)";

                var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);
                cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);
                cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                cmd.Parameters.AddWithValue("@Plan", model.TreatmentPlan ?? "");
                cmd.Parameters.AddWithValue("@Lab", model.LabTestRequested);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["Success"] = "Diagnostic added successfully.";
            return RedirectToAction("LabAndDiagnostics");
        }


        [HttpGet]
        public IActionResult EditDiagnostic(int appointmentId)
        {
            ClinicalNote? note = null;
            using (var con = new SqlConnection(cc))
            {
                var cmd = new SqlCommand(@"
            SELECT * FROM ClinicalNotes 
            WHERE AppointmentId = @aid", con);
                cmd.Parameters.AddWithValue("@aid", appointmentId);
                con.Open();
                var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    note = new ClinicalNote
                    {
                        AppointmentId = appointmentId,
                        DoctorId = Convert.ToInt32(dr["DoctorId"]),
                        Notes = dr["Notes"].ToString(),
                        TreatmentPlan = dr["TreatmentPlan"].ToString(),
                        LabTestRequested = Convert.ToBoolean(dr["LabTestRequested"])
                    };
                }
            }

            if (note == null) return NotFound();
            return View(note);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDiagnostic(ClinicalNote model)
        {
            if (!ModelState.IsValid) return View(model);

            using (var con = new SqlConnection(cc))
            {
                var cmd = new SqlCommand(@"
            UPDATE ClinicalNotes 
            SET Notes = @Notes, TreatmentPlan = @Plan, LabTestRequested = @Lab 
            WHERE AppointmentId = @AppointmentId", con);

                cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                cmd.Parameters.AddWithValue("@Plan", model.TreatmentPlan ?? "");
                cmd.Parameters.AddWithValue("@Lab", model.LabTestRequested);
                cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("LabAndDiagnostics");
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


        private int? GetLoggedInUserId()
        {
            var s = HttpContext.Session.GetString("UserId");
            return int.TryParse(s, out var id) ? id : (int?)null;
        }

        private IActionResult RedirectToLogin()
            => RedirectToAction("Index", "Home");

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!GetLoggedInUserId().HasValue)
                return RedirectToLogin();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string currentPassword, string Password, string confirmPassword)
        {
            var userId = GetLoggedInUserId();
            if (!userId.HasValue)
                return RedirectToLogin();


            if (string.IsNullOrWhiteSpace(currentPassword)
             || string.IsNullOrWhiteSpace(Password)
             || Password != confirmPassword)
            {
                ModelState.AddModelError("", "All fields are required and the new passwords must match.");
                return View();
            }

            string storedPassword;

            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand("SELECT HashedPassword FROM Users WHERE UserID = @Uid", con))
            {
                cmd.Parameters.AddWithValue("@Uid", userId.Value);
                con.Open();
                storedPassword = cmd.ExecuteScalar() as string;
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, storedPassword))
            {
                ModelState.AddModelError("", "Incorrect current password.");
                return View();
            }



            string newHashed = BCrypt.Net.BCrypt.HashPassword(Password);
            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand(
                "UPDATE Users SET Password = @Pwd,HashedPassword = @HashedPassword WHERE UserID = @Uid", con))
            {
                cmd.Parameters.AddWithValue("@HashedPassword", newHashed);
                cmd.Parameters.AddWithValue("@Pwd", Password);
                cmd.Parameters.AddWithValue("@Uid", userId.Value);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["Success"] = "Your password has been changed successfully.";
            return RedirectToAction("Dashboard");
        }

        public record AppointmentDto(
    int Id, string PatientName, DateTime Date, TimeSpan Time, string Status, string Notes);

        [HttpGet("api/staff/schedule")]
        [Produces("application/json")]
        public IActionResult ApiSchedule(DateTime? date = null, int? doctorId = null)
        {
            doctorId ??= GetLoggedInUserId();
            if (doctorId is null) return Unauthorized();

            var items = new List<AppointmentDto>();
            const string sql = @"
        SELECT a.Id, u.Username AS PatientName, a.AppointmentDate, a.AppointmentTime, a.Status, a.Notes
        FROM Appointments a
        JOIN Users u ON a.PatientId = u.UserID
        WHERE a.DoctorId = @did
          AND (@d IS NULL OR a.AppointmentDate = @d)
        ORDER BY a.AppointmentDate, a.AppointmentTime;
    ";

            using var con = new SqlConnection(cc);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@did", doctorId);
            cmd.Parameters.AddWithValue("@d", (object?)date ?? DBNull.Value);
            con.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
                items.Add(new AppointmentDto(
                    (int)dr["Id"],
                    dr["PatientName"].ToString()!,
                    (DateTime)dr["AppointmentDate"],
                    (TimeSpan)dr["AppointmentTime"],
                    dr["Status"].ToString()!,
                    dr["Notes"].ToString()!
                ));

            return Ok(items);
        }

        public class ClinicalNoteCreateDto
        {
            public int AppointmentId { get; set; }
            public int? DoctorId { get; set; } 
            public string? Notes { get; set; }
            public string? TreatmentPlan { get; set; }
            public bool LabTestRequested { get; set; }
        }

        [HttpPost("api/staff/clinical-notes")]
        [Produces("application/json")]
        public IActionResult ApiCreateClinicalNote([FromBody] ClinicalNoteCreateDto dto)
        {
            var did = dto.DoctorId ?? GetLoggedInUserId();
            if (did is null) return Unauthorized();
            if (dto.AppointmentId <= 0) return BadRequest("Invalid appointment.");

            const string sql = @"
        INSERT INTO ClinicalNotes (AppointmentId, DoctorId, Notes, TreatmentPlan, LabTestRequested)
        VALUES (@aid, @did, @n, @t, @lab);
    ";

            using var con = new SqlConnection(cc);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@aid", dto.AppointmentId);
            cmd.Parameters.AddWithValue("@did", did);
            cmd.Parameters.AddWithValue("@n", (object?)dto.Notes ?? "");
            cmd.Parameters.AddWithValue("@t", (object?)dto.TreatmentPlan ?? "");
            cmd.Parameters.AddWithValue("@lab", dto.LabTestRequested);
            con.Open();
            cmd.ExecuteNonQuery();

            return Created("", new { ok = true });
        }
        public record LabRequestDto(int AppointmentId, string PatientName, string Notes);

        [HttpGet("api/staff/lab-requests")]
        [Produces("application/json")]
        public IActionResult ApiLabRequests(int? doctorId = null)
        {
            doctorId ??= GetLoggedInUserId();
            if (doctorId is null) return Unauthorized();

            var list = new List<LabRequestDto>();
            const string sql = @"
        SELECT C.AppointmentId, U.Email AS PatientName, C.Notes
        FROM ClinicalNotes C
        JOIN Appointments A ON C.AppointmentId = A.Id
        JOIN Users U ON A.PatientId = U.UserID
        WHERE C.LabTestRequested = 1 AND C.DoctorId = @did
        ORDER BY C.AppointmentId DESC;
    ";

            using var con = new SqlConnection(cc);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@did", doctorId);
            con.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
                list.Add(new LabRequestDto(
                    (int)dr["AppointmentId"],
                    dr["PatientName"].ToString()!,
                    dr["Notes"].ToString()!
                ));

            return Ok(list);
        }


    }
}

