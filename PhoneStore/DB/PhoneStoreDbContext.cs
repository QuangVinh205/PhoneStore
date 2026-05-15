using Microsoft.EntityFrameworkCore;
using PhoneStore.Models;

namespace PhoneStore.DB
{
    public class PhoneStoreDbContext : DbContext
    {
        public PhoneStoreDbContext(DbContextOptions options) : base(options)
        {
        }

        protected PhoneStoreDbContext()
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<OrdersDetails> OrdersDetails { get; set; }
    }
}
