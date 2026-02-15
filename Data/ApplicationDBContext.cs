using Microsoft.EntityFrameworkCore;
using crud.Models;

namespace crud.Data 
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        :base(options)
        {
        }

        public DbSet<UberTrip> UberTrips {get;set;}
        
    }
}