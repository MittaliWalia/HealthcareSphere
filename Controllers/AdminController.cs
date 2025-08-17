using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using HealthSphere_CapstoneProject.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;
using Rotativa.AspNetCore;
using Rotativa.AspNetCore.Options;


namespace HealthSphere_CapstoneProject.Controllers
{
    public class AdminController : Controller
    {
        private readonly string cc;


        public AdminController(IConfiguration configuration)
        {
            cc = configuration.GetConnectionString("MyConnectionString");
        }
        

        public IActionResult Index()
        {
            List<Login> Users = new List<Login>();

            using (SqlConnection con = new SqlConnection(cc))
            {
                con.Open();
                string query = "SELECT * FROM Users";
                SqlCommand cmd = new SqlCommand(query, con);
                
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Users.Add(new Login
                    {
                        Id = Convert.ToInt32(dr["UserID"]),
                        Email = dr["Email"].ToString(),
                        Username = dr["Username"].ToString(),
                        Role = dr["Role"].ToString()
                    });
                }
            }

            return View(Users);
        }

        

        public IActionResult EditUser(int Id)
        {
            Login user = new Login();

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = "SELECT * FROM Users WHERE UserID = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", Id);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    user.Id = Convert.ToInt32(dr["UserID"]);
                    user.Email = dr["Email"].ToString();
                    user.Username = dr["Username"].ToString();
                    user.Role = dr["Role"].ToString();
                }
            }

            return View(user);
        }

        [HttpPost]
        public IActionResult EditUser(Login model)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = "UPDATE Users SET Email = @Email, Username = @Username, Role = @Role WHERE UserID = @UserId";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", model.Email);
                cmd.Parameters.AddWithValue("@Username", model.Username);
                cmd.Parameters.AddWithValue("@Role", model.Role);
                cmd.Parameters.AddWithValue("@UserId", model.Id);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        public IActionResult DeleteUser(int id)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = "DELETE FROM Users WHERE UserID = @UserId";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@UserId", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }
       
        public IActionResult ManageUsers(string search, string role)
        {
            var users = new List<Login>();

            const string sql = @"
        SELECT UserID, Email, Username, Role
        FROM Users
        WHERE (@search IS NULL OR Email    LIKE '%' + @search + '%'
                          OR Username LIKE '%' + @search + '%')
          AND (@role   IS NULL OR Role = @role)
        ORDER BY Username
    ";

            using var con = new SqlConnection(cc);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@search", (object)search ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@role", (object)role ?? DBNull.Value);

            con.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                users.Add(new Login
                {
                    Id = (int)dr["UserID"],
                    Email = dr["Email"].ToString(),
                    Username = dr["Username"].ToString(),
                    Role = dr["Role"].ToString()
                });
            }

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentRole = role;
            ViewBag.AllRoles = new[] { "Admin", "Doctor", "Nurse", "Patient" };
            return View(users);
        }

        public IActionResult Appointments(DateTime? from, DateTime? to)
        {
            var list = new List<Appointment>();
            const string sql = @"
        SELECT 
          a.Id,
          u.Username AS Patient,
          d.Username AS Doctor,
          a.AppointmentDate,
          a.AppointmentTime,
          a.Status
        FROM Appointments a
        JOIN Users u ON a.PatientId = u.UserID
        JOIN Users d ON a.DoctorId  = d.UserID
        WHERE (@from IS NULL OR a.AppointmentDate >= @from)
          AND (@to   IS NULL OR a.AppointmentDate <= @to)
        ORDER BY a.AppointmentDate DESC, a.AppointmentTime
    ";

            using var con = new SqlConnection(cc);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@from", (object)from ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@to", (object)to ?? DBNull.Value);

            con.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(new Appointment
                {
                    Id = (int)dr["Id"],
                    PatientName = dr["Patient"].ToString(),
                    DoctorName = dr["Doctor"].ToString(),
                    AppointmentDate = (DateTime)dr["AppointmentDate"],
                    AppointmentTime = (TimeSpan)dr["AppointmentTime"],
                    Status = dr["Status"].ToString()
                });
            }

            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

            return View(list);
        }
        public IActionResult AppointmentsPdf(DateTime? from, DateTime? to)
        {
      
            var model = GetAppointments(from, to);

           
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

           
            return new ViewAsPdf("AppointmentsPdf", model)
            {
                FileName = $"appointments_{DateTime.UtcNow:yyyyMMdd}.pdf",
                PageOrientation = Orientation.Landscape,
                PageSize = Size.A4,
                CustomSwitches = "--print-media-type" 
            };
        }

       
        private List<Appointment> GetAppointments(DateTime? from, DateTime? to)
        {
            var list = new List<Appointment>();
            const string sql = @"
            SELECT 
              a.Id,
              u.Username    AS PatientName,
              d.Username    AS DoctorName,
              a.AppointmentDate,
              a.AppointmentTime,
              a.Status
            FROM Appointments a
            JOIN Users u ON a.PatientId = u.UserID
            JOIN Users d ON a.DoctorId  = d.UserID
            WHERE (@from IS NULL OR a.AppointmentDate >= @from)
              AND (@to   IS NULL OR a.AppointmentDate <= @to)
            ORDER BY a.AppointmentDate DESC, a.AppointmentTime
        ";

            using var con = new SqlConnection(cc);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@from", (object)from ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@to", (object)to ?? DBNull.Value);

            con.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                list.Add(new Appointment
                {
                    Id = (int)dr["Id"],
                    PatientName = dr["PatientName"].ToString(),
                    DoctorName = dr["DoctorName"].ToString(),
                    AppointmentDate = (DateTime)dr["AppointmentDate"],
                    AppointmentTime = (TimeSpan)dr["AppointmentTime"],
                    Status = dr["Status"].ToString()
                });
            }
            return list;
        }
        [HttpGet]
        public IActionResult CreateAppointment()
        {
            PopulateDropdowns();
            return View(new Appointment());
        }

        [HttpPost]
        public IActionResult CreateAppointment(Appointment model)
        {
            if (!ModelState.IsValid)
            {
                PopulateDropdowns();
                return View(model);
            }

            using (var con = new SqlConnection(cc))
            {
                const string query = @"
                INSERT INTO Appointments 
                    (PatientId, DoctorId, AppointmentDate, AppointmentTime, Status, Notes)
                VALUES 
                    (@PatientId, @DoctorId, @Date, @Time, @Status, @Notes)";

                using (var cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@PatientId", model.PatientId);
                    cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);
                    cmd.Parameters.AddWithValue("@Date", model.AppointmentDate);
                    cmd.Parameters.AddWithValue("@Time", model.AppointmentTime);
                    cmd.Parameters.AddWithValue("@Status", model.Status);
                    cmd.Parameters.AddWithValue("@Notes", model.Notes ?? string.Empty);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Appointments");
        }

        private void PopulateDropdowns(int? selectedPatientId = null, int? selectedDoctorId = null)
        {
         
            var patients = new List<SelectListItem>();
            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand("SELECT UserID, Email FROM Users WHERE Role='Patient'", con))
            {
                con.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                    {
                        var id = (int)dr["UserID"];
                        patients.Add(new SelectListItem
                        {
                            Value = id.ToString(),
                            Text = dr["Email"].ToString(),
                            Selected = (selectedPatientId.HasValue && selectedPatientId.Value == id)
                        });
                    }
            }
            ViewBag.Patients = patients;

            var doctors = new List<SelectListItem>();
            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand(
                "SELECT UserID, Email FROM Users WHERE Role='Staff'", con))
            {
                con.Open();
                using (var dr = cmd.ExecuteReader())
                    while (dr.Read())
                    {
                        var id = (int)dr["UserID"];
                        doctors.Add(new SelectListItem
                        {
                            Value = id.ToString(),
                            Text = dr["Email"].ToString(),
                            Selected = (selectedDoctorId.HasValue && selectedDoctorId.Value == id)
                        });
                    }
            }
            ViewBag.Doctors = doctors;
        }

        private List<Login> GetUsersByRole(string role)
        {
            List<Login> users = new List<Login>();
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = "SELECT UserID, Email FROM Users WHERE Role = @Role";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Role", role);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    users.Add(new Login
                    {
                        Id = Convert.ToInt32(dr["UserID"]),
                        Email = dr["Email"].ToString()
                    });
                }
            }
            return users;
        }

        [HttpGet]
        public IActionResult EditAppointment(int id)
        {
            var model = new Appointment { };

            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand(
                "SELECT PatientId, DoctorId, AppointmentDate, AppointmentTime, Status, Notes " +
                "FROM Appointments WHERE Id = @Id", con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                using (var dr = cmd.ExecuteReader())
                    if (dr.Read())
                    {
                        model.PatientId = (int)dr["PatientId"];
                        model.DoctorId = (int)dr["DoctorId"];
                        model.AppointmentDate = (DateTime)dr["AppointmentDate"];
                        model.AppointmentTime = (TimeSpan)dr["AppointmentTime"];
                        model.Status = dr["Status"].ToString();
                        model.Notes = dr["Notes"].ToString();
                    }
            }

            PopulateDropdowns(model.PatientId, model.DoctorId);

            return View(model);
        }

        [HttpPost]
        public IActionResult EditAppointment(Appointment model)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"UPDATE Appointments 
                         SET PatientId = @PatientId, DoctorId = @DoctorId, AppointmentDate = @Date,
                             AppointmentTime = @Time, Status = @Status, Notes = @Notes 
                         WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PatientId", model.PatientId);
                cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);
                cmd.Parameters.AddWithValue("@Date", model.AppointmentDate);
                cmd.Parameters.AddWithValue("@Time", model.AppointmentTime);
                cmd.Parameters.AddWithValue("@Status", model.Status);
                cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                cmd.Parameters.AddWithValue("@Id", model.Id);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Appointments");
        }
        public IActionResult DeleteAppointment(int id)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = "DELETE FROM Appointments WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Appointments");
        }
        private void PopulatePatientDropdown(int? selectedPatientId = null)
        {
            var items = new List<SelectListItem>();
            const string sql = @"
        SELECT UserID, Email
        FROM Users
        WHERE Role = 'Patient'
        ORDER BY Email";

            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand(sql, con))
            {
                con.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var id = (int)dr["UserID"];
                        var email = dr["Email"].ToString();
                        items.Add(new SelectListItem
                        {
                            Value = id.ToString(),
                            Text = email,
                            Selected = (selectedPatientId.HasValue && selectedPatientId.Value == id)
                        });
                    }
                }
            }

            ViewBag.Patients = items;
        }
        [HttpPost]
        public IActionResult MarkAsPaid(int billId)
        {
            var userId = GetLoggedInUserId();
            if (!userId.HasValue) return RedirectToLogin();

            using (var con = new SqlConnection(cc))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
UPDATE Bills 
SET Status = 'Paid' 
WHERE Id = @Id 
  ", con))
                {
                    cmd.Parameters.AddWithValue("@Id", billId);
                   
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Billing");
        }
        public IActionResult Billing()
        {
            List<Bill> bills = new List<Bill>();
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
            SELECT B.*, U.Username AS PatientName, A.AppointmentDate 
            FROM Bills B
            JOIN Users U ON B.PatientId = U.UserID
            JOIN Appointments A ON B.AppointmentId = A.Id";

                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    bills.Add(new Bill
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        AppointmentId = Convert.ToInt32(dr["AppointmentId"]),
                        PatientId = Convert.ToInt32(dr["PatientId"]),
                        Amount = Convert.ToDecimal(dr["Amount"]),
                        Status = dr["Status"].ToString(),
                        InsuranceProvider = dr["InsuranceProvider"].ToString(),
                        InsuranceNumber = dr["InsuranceNumber"].ToString(),
                        DateIssued = Convert.ToDateTime(dr["DateIssued"]),
                        Notes = dr["Notes"].ToString(),
                        PatientName = dr["PatientName"].ToString(),
                        AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"])
                    });
                }
            }

            return View(bills);
        }
        public IActionResult CreateBill()
        {
            List<Appointment> completedAppointments = new List<Appointment>();

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
            SELECT A.Id, A.AppointmentDate, U.Username AS PatientName
            FROM Appointments A
            JOIN Users U ON A.PatientId = U.UserID
            WHERE A.Status = 'Completed'";

                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    completedAppointments.Add(new Appointment
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]),
                        PatientName = dr["PatientName"].ToString()
                    });
                }
            }

            ViewBag.CompletedAppointments = completedAppointments;
            PopulateBillDropdowns();
            PopulatePatientDropdown();
            return View();
        }

        [HttpPost]
        public IActionResult CreateBill(Bill model)
        {
            if (!ModelState.IsValid)
            {
                PopulatePatientDropdown();
                PopulateBillDropdowns();
                return View(model);
            }

            int patientId;
            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand(
                "SELECT PatientId FROM Appointments WHERE Id = @Aid", con))
            {
                cmd.Parameters.AddWithValue("@Aid", model.AppointmentId);
                con.Open();
                var obj = cmd.ExecuteScalar();
                if (obj == null)
                {
                    ModelState.AddModelError("", "Selected appointment no longer exists.");
                    PopulateBillDropdowns();
                    PopulatePatientDropdown();
                    return View(model);
                }
                patientId = (int)obj;
            }

            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand(@"
        INSERT INTO Bills
           (AppointmentId, PatientId, Amount, Status,
            InsuranceProvider, InsuranceNumber, DateIssued, Notes)
        VALUES
           (@Aid, @Pid, @Amt, @Status, @Prov, @Num, @Issued, @Notes)
    ", con))
            {
                cmd.Parameters.AddWithValue("@Aid", model.AppointmentId);
                cmd.Parameters.AddWithValue("@Pid", patientId);
                cmd.Parameters.AddWithValue("@Amt", model.Amount);
                cmd.Parameters.AddWithValue("@Status", model.Status);
                cmd.Parameters.AddWithValue("@Prov", model.InsuranceProvider ?? "");
                cmd.Parameters.AddWithValue("@Num", model.InsuranceNumber ?? "");
                cmd.Parameters.AddWithValue("@Issued", DateTime.Today);
                cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");

                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Billing");
        }
        private void PopulateBillDropdowns(int? selectedAppointmentId = null)
        {
            var items = new List<SelectListItem>();

            const string sql = @"
        SELECT 
            a.Id, 
            u.Email AS PatientEmail, 
            a.AppointmentDate
        FROM Appointments a
        INNER JOIN Users u 
            ON a.PatientId = u.UserID
        WHERE a.Status = 'Completed'
        ORDER BY a.AppointmentDate DESC";

            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand(sql, con))
            {
                con.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var id = (int)dr["Id"];
                        var email = dr["PatientEmail"].ToString();
                        var date = ((DateTime)dr["AppointmentDate"]).ToString("yyyy-MM-dd");

                        items.Add(new SelectListItem
                        {
                            Value = id.ToString(),
                            Text = $"{id} – {email} ({date})",
                            Selected = (selectedAppointmentId.HasValue && selectedAppointmentId.Value == id)
                        });
                    }
                }
            }

            ViewBag.CompletedAppointments = items;
        }
        [HttpGet]
        public IActionResult EditBill(int id)
        {
            var model = new Bill();
            const string sql = @"
            SELECT 
                AppointmentId, 
                PatientId, 
                Amount, 
                Status, 
                InsuranceProvider, 
                InsuranceNumber, 
                DateIssued, 
                Notes
            FROM Bills
            WHERE Id = @Id";

            using (var con = new SqlConnection(cc))
            using (var cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        model.Id = id;
                        model.AppointmentId = (int)dr["AppointmentId"];
                        model.PatientId = (int)dr["PatientId"];
                        model.Amount = (decimal)dr["Amount"];
                        model.Status = dr["Status"].ToString();
                        model.InsuranceProvider = dr["InsuranceProvider"].ToString();
                        model.InsuranceNumber = dr["InsuranceNumber"].ToString();
                        model.DateIssued = (DateTime)dr["DateIssued"];
                        model.Notes = dr["Notes"].ToString();
                    }
                    else
                    {
                       
                        return NotFound();
                    }
                }
            }

            PopulatePatientDropdown(model.PatientId);
            PopulateBillDropdowns(model.AppointmentId);

            return View(model);
        }

        [HttpPost]
        public IActionResult EditBill(Bill model)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
            UPDATE Bills SET
            Amount = @Amount,
            Status = @Status,
            InsuranceProvider = @InsuranceProvider,
            InsuranceNumber = @InsuranceNumber,
            Notes = @Notes
            WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Amount", model.Amount);
                cmd.Parameters.AddWithValue("@Status", model.Status);
                cmd.Parameters.AddWithValue("@InsuranceProvider", model.InsuranceProvider ?? "");
                cmd.Parameters.AddWithValue("@InsuranceNumber", model.InsuranceNumber ?? "");
                cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                cmd.Parameters.AddWithValue("@Id", model.Id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            PopulatePatientDropdown(model.PatientId);
            PopulateBillDropdowns(model.AppointmentId);
            return View(model);
            return RedirectToAction("Billing");
        }
        public IActionResult DeleteBill(int id)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = "DELETE FROM Bills WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Billing");
        }
        public IActionResult Dashboard()
        {
            int totalPatients = 0;
            int completedAppointments = 0;
            decimal totalRevenue = 0;

            using (SqlConnection con = new SqlConnection(cc))
            {
                con.Open();

                SqlCommand cmd1 = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Role = 'Patient'", con);
                totalPatients = (int)cmd1.ExecuteScalar();

                SqlCommand cmd2 = new SqlCommand("SELECT COUNT(*) FROM Appointments WHERE Status = 'Completed'", con);
                completedAppointments = (int)cmd2.ExecuteScalar();

               
                SqlCommand cmd3 = new SqlCommand("SELECT ISNULL(SUM(Amount), 0) FROM Bills WHERE Status = 'Paid'", con);
                totalRevenue = Convert.ToDecimal(cmd3.ExecuteScalar());
            }

            ViewBag.TotalPatients = totalPatients;
            ViewBag.CompletedAppointments = completedAppointments;
            ViewBag.TotalRevenue = totalRevenue;

            return View();
        }
        public JsonResult GetMonthlyRevenue()
        {
            List<object> data = new List<object>();
            List<string> months = new List<string>();
            List<decimal> revenues = new List<decimal>();

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
            SELECT DATENAME(MONTH, DateIssued) AS [Month], SUM(Amount) AS Total
            FROM Bills
            WHERE Status = 'Paid'
            GROUP BY DATENAME(MONTH, DateIssued), MONTH(DateIssued)
            ORDER BY MONTH(DateIssued)";

                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    months.Add(dr["Month"].ToString());
                    revenues.Add(Convert.ToDecimal(dr["Total"]));
                }
            }

            data.Add(months);
            data.Add(revenues);

            return Json(data);
        }

        public FileResult ExportBillsToPDF()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document();
                PdfWriter.GetInstance(doc, ms).CloseStream = false;
                doc.Open();

                PdfPTable table = new PdfPTable(5);
                table.AddCell("Patient ID");
                table.AddCell("Amount");
                table.AddCell("Status");
                table.AddCell("Insurance Provider");
                table.AddCell("Date Issued");

                using (SqlConnection con = new SqlConnection(cc))
                {
                    string query = "SELECT PatientId, Amount, Status, InsuranceProvider, DateIssued FROM Bills";
                    SqlCommand cmd = new SqlCommand(query, con);
                    con.Open();
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        table.AddCell(dr["PatientId"].ToString());
                        table.AddCell(dr["Amount"].ToString());
                        table.AddCell(dr["Status"].ToString());
                        table.AddCell(dr["InsuranceProvider"].ToString());
                        table.AddCell(Convert.ToDateTime(dr["DateIssued"]).ToShortDateString());
                    }
                }

                doc.Add(table);
                doc.Close();

                ms.Position = 0;
                return File(ms.ToArray(), "application/pdf", "BillsReport.pdf");
            }
        }
        public FileResult ExportBillsToExcel()
{
    using (var workbook = new ClosedXML.Excel.XLWorkbook())
    {
        var worksheet = workbook.Worksheets.Add("Bills Report");
        worksheet.Cell(1, 1).Value = "Patient ID";
        worksheet.Cell(1, 2).Value = "Amount";
        worksheet.Cell(1, 3).Value = "Status";
        worksheet.Cell(1, 4).Value = "Insurance Provider";
        worksheet.Cell(1, 5).Value = "Date Issued";

        using (SqlConnection con = new SqlConnection(cc))
        {
            string query = "SELECT PatientId, Amount, Status, InsuranceProvider, DateIssued FROM Bills";
            SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            int row = 2;
            while (dr.Read())
            {
                worksheet.Cell(row, 1).Value = dr["PatientId"].ToString();
                worksheet.Cell(row, 2).Value = Convert.ToDecimal(dr["Amount"]);
                worksheet.Cell(row, 3).Value = dr["Status"].ToString();
                worksheet.Cell(row, 4).Value = dr["InsuranceProvider"].ToString();
                worksheet.Cell(row, 5).Value = Convert.ToDateTime(dr["DateIssued"]).ToShortDateString();
                row++;
            }
        }

        using (var stream = new MemoryStream())
        {
            workbook.SaveAs(stream);
            stream.Position = 0;
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "BillsReport.xlsx");
        }
    }
}

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!GetLoggedInUserId().HasValue)
                return RedirectToLogin();
            return View();
        }


        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult ChangePassword(string currentPassword, string Password, string ConfirmPassword)
        {
            
            var userId = GetLoggedInUserId();
            if (!userId.HasValue) return RedirectToLogin();

            
            if (string.IsNullOrWhiteSpace(currentPassword)
             || string.IsNullOrWhiteSpace(Password)
             || Password != ConfirmPassword)
            {
                ModelState.AddModelError("", "All fields are required and new passwords must match.");
                return View();
            }

            string storedHash;
            using (var con = new SqlConnection(cc))
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
            using (var con = new SqlConnection(cc))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
UPDATE Users 
   SET Password = @Password,HashedPassword = @HashedPassword 
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
        public IActionResult ExportUsersCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Id,Email,Username,Role");

            const string sql = "SELECT UserID, Email, Username, Role FROM Users ORDER BY UserID";
            using var con = new SqlConnection(cc);
            using var cmd = new SqlCommand(sql, con);
            con.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                sb.AppendFormat("{0},{1},{2},{3}\n",
                    dr["UserID"],
                    dr["Email"],
                    dr["Username"],
                    dr["Role"]
                );
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"users_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        private IActionResult RedirectToLogin()
            => RedirectToAction("Index", "Home");


        public record UserDto(int Id, string Email, string Username, string Role);

        [HttpGet("api/admin/users")]
        [Produces("application/json")]
        public IActionResult ApiGetUsers(string? search, string? role, int page = 1, int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            var users = new List<UserDto>();
            int total = 0;

            const string sql = @"
        ;WITH F AS (
          SELECT UserID, Email, Username, Role
          FROM Users
          WHERE (@search IS NULL OR Email LIKE '%' + @search + '%' OR Username LIKE '%' + @search + '%')
            AND (@role   IS NULL OR Role = @role)
        )
        SELECT COUNT(*) FROM F;

        SELECT UserID, Email, Username, Role
        FROM F
        ORDER BY Username
        OFFSET (@off) ROWS FETCH NEXT (@take) ROWS ONLY;
    ";

            using var con = new SqlConnection(cc);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@search", (object?)search ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@role", (object?)role ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@off", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@take", pageSize);

            con.Open();
            total = (int)cmd.ExecuteScalar()!;
            using var dr = cmd.ExecuteReader(); 
            dr.NextResult();
            while (dr.Read())
                users.Add(new UserDto((int)dr["UserID"], dr["Email"].ToString()!, dr["Username"].ToString()!, dr["Role"].ToString()!));

            return Ok(new { page, pageSize, total, items = users });
        }

        public class CreateUserDto
        {
            public string Email { get; set; } = "";
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";   
            public string Role { get; set; } = "Patient";
        }

        [HttpPost("api/admin/users")]
        [Produces("application/json")]
        public IActionResult ApiCreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Missing required fields.");

            int newId;
            const string sql = @"
        INSERT INTO Users (Email, Username, Password, Role)
        OUTPUT INSERTED.UserID
        VALUES (@e, @u, @p, @r);";

            using var con = new SqlConnection(cc);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@e", dto.Email);
            cmd.Parameters.AddWithValue("@u", dto.Username);
            cmd.Parameters.AddWithValue("@p", dto.Password); 
            cmd.Parameters.AddWithValue("@r", dto.Role);
            con.Open();
            newId = (int)cmd.ExecuteScalar()!;

            return Created($"/api/admin/users/{newId}", new { id = newId });
        }
        public record RevenuePointDto(string Month, decimal Total);

        [HttpGet("api/admin/revenue/monthly")]
        [Produces("application/json")]
        public IActionResult ApiMonthlyRevenue(int? year = null)
        {
            var items = new List<RevenuePointDto>();
            const string sql = @"
        SELECT FORMAT(DateIssued, 'yyyy-MM') AS Ym, SUM(Amount) AS Total
        FROM Bills
        WHERE Status = 'Paid' AND (@y IS NULL OR YEAR(DateIssued) = @y)
        GROUP BY FORMAT(DateIssued, 'yyyy-MM')
        ORDER BY Ym;
    ";

            using var con = new SqlConnection(cc);
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@y", (object?)year ?? DBNull.Value);
            con.Open();
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
                items.Add(new RevenuePointDto(dr["Ym"].ToString()!, (decimal)dr["Total"]));

            return Ok(items);
        }

    }


}
