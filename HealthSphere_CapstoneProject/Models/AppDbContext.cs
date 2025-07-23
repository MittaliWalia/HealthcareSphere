using Microsoft.EntityFrameworkCore;

namespace HealthSphere_CapstoneProject.Models
{
   

        public class AppDbContext : DbContext
        {
            public AppDbContext(DbContextOptions<AppDbContext> options)
                : base(options) { }

            public DbSet<Login> Users { get; set; }
        }
    
}
