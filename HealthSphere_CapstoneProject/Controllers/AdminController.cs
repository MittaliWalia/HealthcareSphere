using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using HealthSphere_CapstoneProject.Models;
using iTextSharp.text.pdf;
using iTextSharp.text;

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

        public IActionResult Appointments()
        {
            List<Appointment> list = new List<Appointment>();
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
            SELECT A.*, P.Username AS PatientName, D.Username AS DoctorName
            FROM Appointments A
            INNER JOIN Users P ON A.PatientId = P.UserID
            INNER JOIN Users D ON A.DoctorId = D.UserID";

                SqlCommand cmd = new SqlCommand(query, con);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    list.Add(new Appointment
                    {
                        Id = Convert.ToInt32(dr["Id"]),
                        PatientId = Convert.ToInt32(dr["PatientId"]),
                        DoctorId = Convert.ToInt32(dr["DoctorId"]),
                        AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]),
                        AppointmentTime = (TimeSpan)dr["AppointmentTime"],
                        Status = dr["Status"].ToString(),
                        Notes = dr["Notes"].ToString(),
                        PatientName = dr["PatientName"].ToString(),
                        DoctorName = dr["DoctorName"].ToString()
                    });
                }
            }

            return View(list);
        }

        public IActionResult CreateAppointment()
        {
            ViewBag.Patients = GetUsersByRole("Patient");
            ViewBag.Doctors = GetUsersByRole("Doctor");
            return View();
        }

        [HttpPost]
        public IActionResult CreateAppointment(Appointment model)
        {
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, AppointmentTime, Status, Notes)
                         VALUES (@PatientId, @DoctorId, @Date, @Time, @Status, @Notes)";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@PatientId", model.PatientId);
                cmd.Parameters.AddWithValue("@DoctorId", model.DoctorId);
                cmd.Parameters.AddWithValue("@Date", model.AppointmentDate);
                cmd.Parameters.AddWithValue("@Time", model.AppointmentTime);
                cmd.Parameters.AddWithValue("@Status", model.Status);
                cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                con.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Appointments");
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

        public IActionResult EditAppointment(int id)
        {
            Appointment appt = new Appointment();
            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = "SELECT * FROM Appointments WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    appt.Id = Convert.ToInt32(dr["Id"]);
                    appt.PatientId = Convert.ToInt32(dr["PatientId"]);
                    appt.DoctorId = Convert.ToInt32(dr["DoctorId"]);
                    appt.AppointmentDate = Convert.ToDateTime(dr["AppointmentDate"]);
                    appt.AppointmentTime = (TimeSpan)dr["AppointmentTime"];
                    appt.Status = dr["Status"].ToString();
                    appt.Notes = dr["Notes"].ToString();
                }
            }

            ViewBag.Patients = GetUsersByRole("Patient");
            ViewBag.Doctors = GetUsersByRole("Doctor");
            return View(appt);
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
            return View();
        }

        [HttpPost]
        public IActionResult CreateBill(Bill model)
        {
            model.DateIssued = DateTime.Today;

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = @"
            INSERT INTO Bills 
            (AppointmentId, PatientId, Amount, Status, InsuranceProvider, InsuranceNumber, DateIssued, Notes)
            VALUES 
            (@AppointmentId, @PatientId, @Amount, @Status, @InsuranceProvider, @InsuranceNumber, @DateIssued, @Notes)";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@AppointmentId", model.AppointmentId);
                cmd.Parameters.AddWithValue("@PatientId", model.PatientId);
                cmd.Parameters.AddWithValue("@Amount", model.Amount);
                cmd.Parameters.AddWithValue("@Status", model.Status);
                cmd.Parameters.AddWithValue("@InsuranceProvider", model.InsuranceProvider ?? "");
                cmd.Parameters.AddWithValue("@InsuranceNumber", model.InsuranceNumber ?? "");
                cmd.Parameters.AddWithValue("@DateIssued", model.DateIssued);
                cmd.Parameters.AddWithValue("@Notes", model.Notes ?? "");
                con.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Billing");
        }
        public IActionResult EditBill(int id)
        {
            Bill bill = new Bill();

            using (SqlConnection con = new SqlConnection(cc))
            {
                string query = "SELECT * FROM Bills WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Id", id);
                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    bill.Id = Convert.ToInt32(dr["Id"]);
                    bill.AppointmentId = Convert.ToInt32(dr["AppointmentId"]);
                    bill.PatientId = Convert.ToInt32(dr["PatientId"]);
                    bill.Amount = Convert.ToDecimal(dr["Amount"]);
                    bill.Status = dr["Status"].ToString();
                    bill.InsuranceProvider = dr["InsuranceProvider"].ToString();
                    bill.InsuranceNumber = dr["InsuranceNumber"].ToString();
                    bill.DateIssued = Convert.ToDateTime(dr["DateIssued"]);
                    bill.Notes = dr["Notes"].ToString();
                }
            }

            return View(bill);
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

                // Total Patients
                SqlCommand cmd1 = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Role = 'Patient'", con);
                totalPatients = (int)cmd1.ExecuteScalar();

                // Completed Appointments
                SqlCommand cmd2 = new SqlCommand("SELECT COUNT(*) FROM Appointments WHERE Status = 'Completed'", con);
                completedAppointments = (int)cmd2.ExecuteScalar();

                // Total Revenue
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


    }
    

    }
