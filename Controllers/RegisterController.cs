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

        public IActionResult Register()
        {
            return View();
        }
        

       

        [HttpPost]
        public IActionResult Register(Login model)
        {
            ModelState.Remove(nameof(model.CurrentPassword));
            if (ModelState.IsValid)
            {

                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
                using (SqlConnection con = new SqlConnection(cc))
                {

                    string checkUser = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
                    SqlCommand checkCmd = new SqlCommand(checkUser, con);
                    checkCmd.Parameters.AddWithValue("@Email", model.Email);

                    con.Open();
                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists == 1)
                    {
                        ViewBag.Message = "Email already exists.";
                        return View(model);
                    }


                    string query = "INSERT INTO Users ( Username, Password , HashedPassword ,Role , Email) VALUES ( @Username, @Password,@HashedPassword,@Role , @Email)";
                    SqlCommand cmd = new SqlCommand(query, con);

                    cmd.Parameters.AddWithValue("@Username", model.Username);
                    cmd.Parameters.AddWithValue("@Password", model.Password);
                    cmd.Parameters.AddWithValue("@HashedPassword", hashedPassword);
                    cmd.Parameters.AddWithValue("@Role", model.Role);
                    cmd.Parameters.AddWithValue("@Email", model.Email);
                   

                    cmd.ExecuteNonQuery();
                    con.Close();

                    TempData["Message"] = "Registered successfully.";
                    return RedirectToAction("Index", "Home"); 
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
