using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MudServer.Settings;

namespace MudServer.DAL
{
    public class DataContext : DbContext
    {
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
                optionsBuilder.UseSqlServer(cstr);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
