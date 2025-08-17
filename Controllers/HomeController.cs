using System;
using System.Data;
using HealthSphere_CapstoneProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace HealthSphere_CapstoneProject.Controllers
{
    public class HomeController : Controller
    {

        private readonly string cc;

       
        public HomeController(IConfiguration configuration)
        {
            cc = configuration.GetConnectionString("MyConnectionString");
        }
        protected int? GetLoggedInUserId()
        {
            var s = HttpContext.Session.GetString("UserId");
            return string.IsNullOrEmpty(s) ? null : (int?)int.Parse(s);
        }
        public IActionResult Index()
        {

            string Email = HttpContext.Session.GetString("Email");

            ViewBag.Email = Email;
            return View();
        }

        [HttpPost]
        public IActionResult Index(Login model)
        {
            
            var emailErrors = ModelState[nameof(model.Email)]?.Errors.Count ?? 0;
            var passErrors = ModelState[nameof(model.Password)]?.Errors.Count ?? 0;

            if (emailErrors == 0 && passErrors == 0)
            {
            string newhashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
                using (SqlConnection con = new SqlConnection(cc))
                {
                    string query = @"
                SELECT UserID, Role,Username,HashedPassword 
                FROM Users 
                WHERE Email = @Email AND Password = @Password";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Password", model.Password);
                    con.Open();

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            string hasedPassword = dr["HashedPassword"].ToString();                            
                            int usedrId = Convert.ToInt32(dr["UserID"]);
                            string role = dr["Role"].ToString();
                            string username = dr["Username"].ToString();
                           
                            HttpContext.Session.SetString("Email", model.Email);
                            HttpContext.Session.SetString("Role", role);
                            HttpContext.Session.SetString("UserId", usedrId.ToString());
                            HttpContext.Session.SetString("Username", username);
                            if (BCrypt.Net.BCrypt.Verify(model.Password,hasedPassword))
                            {
                                switch (role)
                                {
                                    case "Admin":
                                        return RedirectToAction("Index", "Admin");
                                    case "Staff":
                                        return RedirectToAction("Dashboard", "Staff");
                                    case "Patient":
                                        return RedirectToAction("Dashboard", "Patient");
                                    default:
                                        ViewBag.Message = "Unknown role. Please contact support.";
                                        break;
                                }
                            }
                        }
                        else
                        {
                            TempData["Message"] = "Invalid username or password.";
                        }
                    }
                }
            }

            return View(model);
        }
      
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }


    }
}
