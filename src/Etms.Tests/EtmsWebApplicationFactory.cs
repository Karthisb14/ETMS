using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Etms.Tests;

// In-process test host: switches Program.cs to EF Core InMemory via environment
// variables (see UseInMemoryDatabaseForTests) instead of registering AddDbContext a
// second time, which would leave both the SqlServer and InMemory providers
// registered. Environment variables are used rather than ConfigureAppConfiguration
// because Program.cs reads configuration to pick a provider *before* WebApplicationFactory's
// deferred ConfigureAppConfiguration callbacks are applied — env vars are visible as
// soon as WebApplication.CreateBuilder(args) runs.
public class EtmsWebApplicationFactory : WebApplicationFactory<Program>
{
    // EF Core's InMemory provider keys databases by name in a process-wide store, so
    // this must be unique per factory instance to keep test classes isolated from
    // each other (xUnit creates one factory instance per IClassFixture-using class).
    private readonly string _databaseName = $"etms-tests-{Guid.NewGuid()}";

    public EtmsWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("UseInMemoryDatabaseForTests", "true");
        Environment.SetEnvironmentVariable("InMemoryDatabaseName", _databaseName);
        Environment.SetEnvironmentVariable("ConnectionStrings__Etms", "Server=unused;Database=unused");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
    }
}


