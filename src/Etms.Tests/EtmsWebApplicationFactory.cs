using Etms.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Etms.Tests;

// In-process test host: swaps the real SQL Server DbContext for EF Core InMemory
// and skips the relational-only startup migration (see Program.cs SkipMigrationOnStartup).
public class EtmsWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Etms"] = "Server=unused;Database=unused",
                ["SkipMigrationOnStartup"] = "true",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<EtmsDbContext>>();
            services.AddDbContext<EtmsDbContext>(options =>
                options.UseInMemoryDatabase($"etms-tests-{Guid.NewGuid()}"));
        });
    }
}
