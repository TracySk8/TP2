using Microsoft.EntityFrameworkCore;

namespace ProductApi
{
    public class ProductDBContext : DbContext
    {
        public ProductDBContext(DbContextOptions<ProductDBContext> options)
        : base(options)
        {
        }
    }
}
