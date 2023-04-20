using Microsoft.EntityFrameworkCore;
using ProductApi.Models;

namespace ProductApi
{
    public class ProductDBContext : DbContext
    {
        public ProductDBContext(DbContextOptions<ProductDBContext> options)
        : base(options)
        {
        }
        public DbSet<Product> Product { get; set; }
        public DbSet<CartProduct> CartProduct { get; set; }
    }
}
