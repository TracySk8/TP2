using Microsoft.EntityFrameworkCore;
using SellerApi.Models;

namespace SellerApi
{
    public class SellerDBContext : DbContext
    {

        public DbSet<Seller> Seller { get; set; }

        public SellerDBContext(DbContextOptions<SellerDBContext> options)
        : base(options)
        {
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder dbContextOptionsBuilder)

        //{

        //    string connection_string = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=master;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        //    string database_name = "sellerServiceDb";

        //    dbContextOptionsBuilder.UseSqlServer($"{connection_string};Database={database_name};");

        //}

    }
}