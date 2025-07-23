using HealthSphere_CapstoneProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HealthSphere_CapstoneProject.Controllers
{
    public class RegisterController : Controller
    {
        private readonly string cc;

        public RegisterController(IConfiguration configuration)
        {
            cc = configuration.GetConnectionString("MyConnectionString");
        }

        public IActionResult Index()
        {
            return View();
        }
        

       

        [HttpPost]
        public IActionResult Index(Login model)
        {
            if (ModelState.IsValid)
            {
               

                using (SqlConnection con = new SqlConnection(cc))
                {

                    string checkUser = "SELECT COUNT(1) FROM Users WHERE username = @username";
                    SqlCommand checkCmd = new SqlCommand(checkUser, con);
                    checkCmd.Parameters.AddWithValue("@username", model.Username);

                    con.Open();
                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists == 1)
                    {
                        ViewBag.Message = "Username already exists.";
                        return View(model);
                    }


                    string query = "INSERT INTO Users ( Username, Password ,Role , Email,Phonenumber) VALUES ( @Username, @Password,@Role , @Email,@PhoneNumber)";
                    SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@Username", model.Username);
                    cmd.Parameters.AddWithValue("@Password", model.Password);
                    cmd.Parameters.AddWithValue("@Role", model.Role);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                    cmd.Parameters.AddWithValue("@Phonenumber", model.Phonenumber);

                    cmd.ExecuteNonQuery();
                    con.Close();

                    ViewBag.Message = "Registered successfully.";
                    return RedirectToAction("Index", "Home"); // Redirect to login
                }
            }

            return View(model);
        }
    }
}
