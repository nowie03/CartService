using CartService.Models;
using Microsoft.EntityFrameworkCore;

namespace CartService.Context
{
    public class ServiceContext : DbContext
    {
        public ServiceContext(DbContextOptions options) : base(options) { }

        public DbSet<Cart> Carts { get; set; }

        public DbSet<CartItem> CartItems { get; set; }

        public DbSet<Message> Outbox { get; set; }

        public DbSet<ConsumedMessage> ConsumedMessages { get; set; }

        override
        protected void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConsumedMessage>().HasIndex(message => message.MessageId).IsUnique();


        }




    }
}
