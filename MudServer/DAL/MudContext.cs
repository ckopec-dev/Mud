using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MudServer.Settings;

namespace MudServer.DAL
{
    public class MudContext : DbContext
    {
        private static string _ConnectionString = String.Empty;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string executableDirectory = AppContext.BaseDirectory;

                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(executableDirectory)
                    .AddJsonFile("appsettings.json")
                    .AddUserSecrets<Program>()
                    .Build();

                string? cstr = configuration.GetSection("AppSettings").Get<AppSettings>()!.DatabaseSettings!.ConnectionString;
                optionsBuilder.UseSqlServer(_ConnectionString);
            }
        }

        public static void SetConnectionString(string connection)
        {
            _ConnectionString = connection;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public virtual DbSet<Item> Items { get; set; }
    }
}
