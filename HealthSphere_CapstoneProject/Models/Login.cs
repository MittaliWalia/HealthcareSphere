using System.ComponentModel.DataAnnotations;

namespace HealthSphere_CapstoneProject.Models
{
    public class Login
    {
       public string? Role { get; set; }
        public string? Username { get; set; }
        
        public string? Password { get; set; }
        public string? Email { get; set; }
        public int? Phonenumber { get; set; }
        public int? Id { get; set; }
       
        
    }
}
