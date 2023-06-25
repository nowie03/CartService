using CartService.Models;
using Microsoft.EntityFrameworkCore;

namespace CartService.Context
{
    public class ServiceContext:DbContext
    {
        public ServiceContext(DbContextOptions options):base(options) { }

        public DbSet<Cart> Cart { get; set; }


    }
}
