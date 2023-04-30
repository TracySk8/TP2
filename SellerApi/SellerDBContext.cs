using Microsoft.EntityFrameworkCore;
using SellerApi.Models;

namespace SellerApi
{
    public class SellerDBContext : DbContext
    {

        public SellerDBContext(DbContextOptions<SellerDBContext> options)
        : base(options)
        {
        }

        public DbSet<Seller> Seller { get; set; }
        public DbSet<SellerStats> SellerStats { get; set; }
        public DbSet<Password> Password { get; set; }

    }
}