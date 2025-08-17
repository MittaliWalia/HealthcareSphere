using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using HealthSphere_CapstoneProject.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace HealthSphere_CapstoneProject.Controllers
{
    public class PatientController : Controller
    {
        private readonly string _connectionString;

        public PatientController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MyConnectionString");
        }

        public IActionResult Dashboard()
        {
            var userId = GetLoggedInUserId();
            if (!userId.HasValue) return RedirectToLogin();

            
            dynamic patient = null;
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
SELECT 
    Username, 
    Email, 
    
    Role
FROM Users
WHERE UserID = @Pid", con))
                {
                    cmd.Parameters.AddWithValue("@Pid", userId.Value);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            patient = new
                            {
                                Username = rdr["Username"].ToString(),
                                Email = rdr["Email"].ToString(),
                               
                                Role = rdr["Role"].ToString()
                            };
                        }
                    }
                }
            }

           
            var appointments = new List<dynamic>();
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
SELECT 
    A.Id, 
    A.AppointmentDate, 
    A.AppointmentTime, 
    A.Status, 
    U.Username AS DoctorName
FROM Appointments A
JOIN Users U 
    ON A.DoctorId = U.UserID
WHERE A.PatientId = @Pid
  AND (A.AppointmentDate > CAST(GETDATE() AS DATE)
       OR (A.AppointmentDate = CAST(GETDATE() AS DATE)
           AND A.AppointmentTime >= CONVERT(time, GETDATE())))
ORDER BY A.AppointmentDate, A.AppointmentTime", con))
                {
                    cmd.Parameters.AddWithValue("@Pid", userId.Value);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            appointments.Add(new
                            {
                                Id = (int)rdr["Id"],
                                Date = ((DateTime)rdr["AppointmentDate"]).ToShortDateString(),
                                Time = ((TimeSpan)rdr["AppointmentTime"]).ToString(),
                                Status = rdr["Status"].ToString(),
                                Doctor = rdr["DoctorName"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.Patient = patient;
            ViewBag.Appointments = appointments;
            return View();
        }

        public IActionResult BookAppointment()
        {
            var userId = GetLoggedInUserId();
            if (!userId.HasValue) return RedirectToLogin();

            var doctors = new List<dynamic>();
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
SELECT UserID, Username 
FROM Users 
WHERE Role = 'Staff'", con))
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        doctors.Add(new
                        {
                            Id = (int)rdr["UserID"],
                            Name = rdr["Username"].ToString()
                        });
                    }
                }
            }
            ViewBag.Doctors = doctors;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult BookAppointment(int doctorId, DateTime appointmentDate, TimeSpan appointmentTime)
        {
            var userId = GetLoggedInUserId();
            if (!userId.HasValue) return RedirectToLogin();

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
INSERT INTO Appointments 
    (PatientId, DoctorId, AppointmentDate, AppointmentTime, Status)
VALUES 
    (@Pid, @Did, @Date, @Time, 'Scheduled')", con))
                {
                    cmd.Parameters.AddWithValue("@Pid", userId.Value);
                    cmd.Parameters.AddWithValue("@Did", doctorId);
                    cmd.Parameters.AddWithValue("@Date", appointmentDate.Date);
                    cmd.Parameters.AddWithValue("@Time", appointmentTime);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public IActionResult CancelAppointment(int appointmentId)
        {
            var userId = GetLoggedInUserId();
            if (!userId.HasValue) return RedirectToLogin();

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
UPDATE Appointments 
SET Status = 'Canceled' 
WHERE Id = @Aid 
  AND PatientId = @Pid", con))
                {
                    cmd.Parameters.AddWithValue("@Aid", appointmentId);
                    cmd.Parameters.AddWithValue("@Pid", userId.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Dashboard");
        }

     
        public IActionResult Bills()
        {
            var userId = GetLoggedInUserId();
            if (!userId.HasValue) return RedirectToLogin();

            var bills = new List<dynamic>();
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
SELECT 
    Id, 
    DateIssued, 
    Amount, 
    Status, 
    InsuranceProvider, 
    InsuranceNumber
FROM Bills
WHERE PatientId = @Pid
ORDER BY DateIssued DESC", con))
                {
                    cmd.Parameters.AddWithValue("@Pid", userId.Value);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            bills.Add(new
                            {
                                Id = (int)rdr["Id"],
                                DateIssued = ((DateTime)rdr["DateIssued"]).ToShortDateString(),
                                Amount = (decimal)rdr["Amount"],
                                Status = rdr["Status"].ToString(),
                                InsuranceProvider = rdr["InsuranceProvider"] as string,
                                InsuranceNumber = rdr["InsuranceNumber"] as string
                            });
                        }
                    }
                }
            }

            ViewBag.Bills = bills;
            return View("Billing");
        }

        [HttpPost]
        public IActionResult MarkAsPaid(int billId)
        {
            var userId = GetLoggedInUserId();
            if (!userId.HasValue) return RedirectToLogin();

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
UPDATE Bills 
SET Status = 'Paid' 
WHERE Id = @Id 
  AND PatientId = @Pid", con))
                {
                    cmd.Parameters.AddWithValue("@Id", billId);
                    cmd.Parameters.AddWithValue("@Pid", userId.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Bills");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult UpdateInsurance(int billId, string insuranceProvider, string insuranceNumber)
        {
            var userId = GetLoggedInUserId();
            if (!userId.HasValue) return RedirectToLogin();

            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
UPDATE Bills
SET InsuranceProvider = @Prov,
    InsuranceNumber   = @Num
WHERE Id = @Id 
  AND PatientId = @Pid", con))
                {
                    cmd.Parameters.AddWithValue("@Prov", insuranceProvider ?? "");
                    cmd.Parameters.AddWithValue("@Num", insuranceNumber ?? "");
                    cmd.Parameters.AddWithValue("@Id", billId);
                    cmd.Parameters.AddWithValue("@Pid", userId.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Bills");
        }

     
        public IActionResult MyRecords()
        {
            var userId = GetLoggedInUserId();
            if (!userId.HasValue) return RedirectToLogin();

            var records = new List<dynamic>();
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
SELECT 
    M.RecordDate, 
    M.Diagnosis, 
    M.Prescription, 
    M.Notes, 
    U.Username AS DoctorName
FROM MedicalRecords M
JOIN Users U 
    ON M.DoctorId = U.UserID
WHERE M.PatientId = @Pid
ORDER BY M.RecordDate DESC", con))
                {
                    cmd.Parameters.AddWithValue("@Pid", userId.Value);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            records.Add(new
                            {
                                RecordDate = ((DateTime)rdr["RecordDate"]).ToString("yyyy-MM-dd"),
                                Diagnosis = rdr["Diagnosis"].ToString(),
                                Prescription = rdr["Prescription"].ToString(),
                                Notes = rdr["Notes"].ToString(),
                                DoctorName = rdr["DoctorName"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.Records = records;
            return View();
        }


        public FileResult DownloadRecordsCsv()
        {
            var userId = GetLoggedInUserId();
            if (!userId.HasValue) return null;

            var lines = new List<string>
            {
                "RecordDate,Diagnosis,Prescription,Notes,DoctorName"
            };
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
SELECT 
    M.RecordDate, 
    M.Diagnosis, 
    M.Prescription, 
    M.Notes, 
    U.Username AS DoctorName
FROM MedicalRecords M
JOIN Users U 
    ON M.DoctorId = U.UserID
WHERE M.PatientId = @Pid
ORDER BY M.RecordDate DESC", con))
                {
                    cmd.Parameters.AddWithValue("@Pid", userId.Value);
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var row = string.Join(",",
                                ((DateTime)rdr["RecordDate"]).ToString("yyyy-MM-dd"),
                                rdr["Diagnosis"].ToString().Replace(",", " "),
                                rdr["Prescription"].ToString().Replace(",", " "),
                                rdr["Notes"].ToString().Replace(",", " "),
                                rdr["DoctorName"].ToString()
                            );
                            lines.Add(row);
                        }
                    }
                }
            }

            var csvBytes = System.Text.Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, lines));
            return File(csvBytes, "text/csv", "MedicalRecords.csv");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!GetLoggedInUserId().HasValue)
                return RedirectToLogin();
            return View();
        }

       
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string currentPassword, string Password, string confirmPassword)
        {
            var userId = GetLoggedInUserId();
            if (!userId.HasValue) return RedirectToLogin();

            
            if (string.IsNullOrWhiteSpace(currentPassword)
             || string.IsNullOrWhiteSpace(Password)
             || Password != confirmPassword)
            {
                ModelState.AddModelError("", "All fields are required and new passwords must match.");
                return View();
            }

            string storedHash;
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(
                    "SELECT HashedPassword FROM Users WHERE UserID = @Uid", con))
                {
                    cmd.Parameters.AddWithValue("@Uid", userId.Value);
                    storedHash = cmd.ExecuteScalar() as string;
                }
            }


            if (!BCrypt.Net.BCrypt.Verify(currentPassword, storedHash))
            {
                ModelState.AddModelError("", "Incorrect current password.");
                return View();
            }



            string newHashed = BCrypt.Net.BCrypt.HashPassword(Password);


            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
    UPDATE Users 
       SET Password = @Password ,HashedPassword = @HashedPassword
     WHERE UserID = @Uid", con))

                {
                    cmd.Parameters.AddWithValue("@HashedPassword", newHashed);
                    cmd.Parameters.AddWithValue("@Password", Password);
                    cmd.Parameters.AddWithValue("@Uid", userId.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Success"] = "Password updated successfully.";
            return RedirectToAction("Dashboard");
        }

        
     
        private int? GetLoggedInUserId()
        {
            var s = HttpContext.Session.GetString("UserId");
            return int.TryParse(s, out var i) ? i : (int?)null;
        }

        private IActionResult RedirectToLogin()
            => RedirectToAction("Index", "Home");
        public record PatientAppointmentDto(
    int Id, string DoctorName, DateTime Date, TimeSpan Time, string Status, string Notes);

        public record PatientBillDto(
            int Id, int AppointmentId, decimal Amount, string Status, string InsuranceProvider,
            string InsuranceNumber, DateTime DateIssued, string Notes);

        public record BookAppointmentDto(int DoctorId, DateTime Date, TimeSpan Time);
        [HttpGet("api/patient/appointments")]
        [Produces("application/json")]
        public IActionResult ApiMyAppointments(DateTime? from = null, DateTime? to = null, string? status = null, int? patientId = null)
        {
            patientId ??= GetLoggedInUserId();
            if (patientId is null) return Unauthorized();

            var list = new List<PatientAppointmentDto>();
            const string sql = @"
        SELECT a.Id,
               d.Username AS DoctorName,
               a.AppointmentDate, a.AppointmentTime,
               a.Status, a.Notes
        FROM Appointments a
        JOIN Users d ON a.DoctorId = d.UserID
        WHERE a.PatientId = @pid
          AND (@from IS NULL OR a.AppointmentDate >= @from)
          AND (@to   IS NULL OR a.AppointmentDate <= @to)
          AND (@st   IS NULL OR a.Status = @st)
        ORDER BY a.AppointmentDate DESC, a.AppointmentTime DESC";

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@pid", patientId);
            cmd.Parameters.AddWithValue("@from", (object?)from ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@to", (object?)to ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@st", (object?)status ?? DBNull.Value);

            con.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(new PatientAppointmentDto(
                    (int)dr["Id"],
                    dr["DoctorName"].ToString()!,
                    (DateTime)dr["AppointmentDate"],
                    (TimeSpan)dr["AppointmentTime"],
                    dr["Status"].ToString()!,
                    dr["Notes"].ToString()!
                ));
            }
            return Ok(list);
        }
        [HttpPost("api/patient/appointments")]
        [Produces("application/json")]
        public IActionResult ApiBookAppointment([FromBody] BookAppointmentDto dto, int? patientId = null)
        {
            patientId ??= GetLoggedInUserId();
            if (patientId is null) return Unauthorized();

            if (dto.DoctorId <= 0 || dto.Date == default || dto.Time == default)
                return BadRequest("DoctorId, Date and Time are required.");

           
            const string checkSql = @"
        SELECT COUNT(*) 
        FROM Appointments
        WHERE DoctorId = @did AND AppointmentDate = @d AND AppointmentTime = @t";
            using (var con = new SqlConnection(_connectionString))
            using (var check = new SqlCommand(checkSql, con))
            {
                check.Parameters.AddWithValue("@did", dto.DoctorId);
                check.Parameters.AddWithValue("@d", dto.Date.Date);
                check.Parameters.AddWithValue("@t", dto.Time);
                con.Open();
                var conflict = (int)check.ExecuteScalar()!;
                if (conflict > 0) return Conflict(new { message = "Selected slot is not available." });
            }

            int newId;
            const string insertSql = @"
        INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, AppointmentTime, Status, Notes)
        OUTPUT INSERTED.Id
        VALUES (@pid, @did, @d, @t, 'Scheduled', 'Booked via API');";

            using (var con = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(insertSql, con))
            {
                cmd.Parameters.AddWithValue("@pid", patientId);
                cmd.Parameters.AddWithValue("@did", dto.DoctorId);
                cmd.Parameters.AddWithValue("@d", dto.Date.Date);
                cmd.Parameters.AddWithValue("@t", dto.Time);
                con.Open();
                newId = (int)cmd.ExecuteScalar()!;
            }

            return Created($"/api/patient/appointments/{newId}", new { id = newId });
        }
        [HttpGet("api/patient/bills")]
        [Produces("application/json")]
        public IActionResult ApiMyBills(string? status = null, int? patientId = null)
        {
            patientId ??= GetLoggedInUserId();
            if (patientId is null) return Unauthorized();

            var items = new List<PatientBillDto>();
            const string sql = @"
        SELECT Id, AppointmentId, Amount, Status,
               InsuranceProvider, InsuranceNumber, DateIssued, Notes
        FROM Bills
        WHERE PatientId = @pid
          AND (@st IS NULL OR Status = @st)
        ORDER BY DateIssued DESC, Id DESC";

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@pid", patientId);
            cmd.Parameters.AddWithValue("@st", (object?)status ?? DBNull.Value);

            con.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                items.Add(new PatientBillDto(
                    (int)dr["Id"],
                    (int)dr["AppointmentId"],
                    (decimal)dr["Amount"],
                    dr["Status"].ToString()!,
                    dr["InsuranceProvider"].ToString()!,
                    dr["InsuranceNumber"].ToString()!,
                    (DateTime)dr["DateIssued"],
                    dr["Notes"].ToString()!
                ));
            }

            return Ok(items);
        }

    }
}
