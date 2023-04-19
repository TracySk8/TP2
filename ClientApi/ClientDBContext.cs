using ClientApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientApi
{
    public class ClientDBContext : DbContext
    {
        public ClientDBContext(DbContextOptions<ClientDBContext> options)
        : base(options)
        {
        }

        public DbSet<Client> Client { get; set; }
        public DbSet<ClientStats> ClientStats { get; set; }
    }
}
