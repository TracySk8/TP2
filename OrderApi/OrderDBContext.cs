using Microsoft.EntityFrameworkCore;
using OrderApi.Models;
using System.Collections.Generic;

namespace OrderApi
{
    public class OrderDBContext : DbContext
    {

        public DbSet<Receipt> Receipt { get; set; }
        public DbSet<ReceiptItem> ReceiptItem { get; set; }

        public OrderDBContext(DbContextOptions<OrderDBContext> options)
        : base(options)
        {
        }

    }
    
}
