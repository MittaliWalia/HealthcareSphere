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

        public IActionResult Index()
        {

            string username = HttpContext.Session.GetString("Username");

            ViewBag.Username = username;
            return View();
        }

        [HttpPost]
        public IActionResult Index(Login model)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection con = new SqlConnection(cc))
                {
                    string query = "SELECT COUNT(1) FROM Users WHERE Username = @Username AND Password = @Password AND Role = @Role" ;
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@username", model.Username);
                    cmd.Parameters.AddWithValue("@password", model.Password);
                    cmd.Parameters.AddWithValue("@Role", model.Role);
                    con.Open();

                  
                    var roleObj = cmd.ExecuteScalar();
                    if (roleObj != null)
                    {
                        string role = roleObj.ToString();

                        // Set session
                        HttpContext.Session.SetString("Username", model.Username);
                        HttpContext.Session.SetString("Role", role);

                        // Role-based redirection
                        switch (role)
                        {
                            case "Admin":
                                return RedirectToAction("Dashboard", "Admin");
                            case "Doctor":
                            case "Nurse":
                                return RedirectToAction("Dashboard", "Staff");
                            case "Patient":
                                return RedirectToAction("Dashboard", "Patient");
                            default:
                                ViewBag.Message = "Unknown role. Contact support.";
                                break;
                        }
                    }
                    else
                    {
                        ViewBag.Message = "Invalid Username or Password.";
                    }
                }
            }

            return View(model);
        }

      
    }
}
