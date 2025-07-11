// Add this new file: Data/DesignTimeDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ChatAppBackend.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ChatAppDbContext>
    {
        public ChatAppDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<ChatAppDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json");
            }

            builder.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                options => options.MigrationsAssembly(typeof(ChatAppDbContext).Assembly.FullName));

            return new ChatAppDbContext(builder.Options);
        }
    }
}