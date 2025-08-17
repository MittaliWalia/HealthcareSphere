using System.ComponentModel.DataAnnotations;

namespace HealthSphere_CapstoneProject.Models
{
    public class Login
    {
  
       public int? Phonenumber { get; set; }
        public int? Id { get; set; }
        [Required, StringLength(50)]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; }
       
        [Required(ErrorMessage = "CurrentPassword is required")]
        public string CurrentPassword { get; set; }
        public string? hashedPassword { get; set; }
    }
}
