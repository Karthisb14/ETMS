using Etms.Data;
using Etms.Web.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string comes from configuration (env var ConnectionStrings__Etms in
// docker-compose / secrets manager in production) — never hard-coded, unlike the
// legacy app's plaintext web.config connection string.
// EF Core InMemory is used only under the test host (Etms.Tests/EtmsWebApplicationFactory),
// which doesn't support relational migrations — switching provider via configuration here
// avoids the "two providers registered" conflict that registering AddDbContext twice
// (once in Program.cs, once in test ConfigureServices) would otherwise cause.
if (builder.Configuration.GetValue<bool>("UseInMemoryDatabaseForTests"))
{
    var inMemoryDatabaseName = builder.Configuration["InMemoryDatabaseName"] ?? "etms-tests";
    builder.Services.AddDbContext<EtmsDbContext>(options =>
        options.UseInMemoryDatabase(inMemoryDatabaseName));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("Etms")
        ?? throw new InvalidOperationException("Connection string 'Etms' not configured.");
    builder.Services.AddDbContext<EtmsDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Kestrel: don't disclose the server/framework via the Server response header (OWASP A05).
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Trust X-Forwarded-* headers from a reverse proxy doing TLS termination in front of
// this container (human decision, 2026-09-19: reverse proxy/load balancer terminates
// TLS, this app runs plain HTTP behind it). KnownNetworks/KnownProxies are cleared
// because the proxy's address isn't known at build time (varies per deployment); the
// container network itself is assumed trusted. Restrict to the actual proxy's address
// for defense-in-depth once that's known for a given environment.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Data Protection keys must survive container restarts (session/token validity) —
// persisted to a volume-mounted path instead of the container's ephemeral filesystem.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        // HIPAA/PII-appropriate password + lockout policy (KAN-16 / KAN-12).
        options.Password.RequiredLength = 12;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<EtmsDbContext>();

// Secure cookie flags (OWASP A02/A07): HttpOnly (already Identity default), Secure,
// and strict SameSite to reduce CSRF/session-hijacking exposure. Short sliding
// expiration is HIPAA's "automatic logoff" technical safeguard — the legacy app had
// no session concept at all, so there was no equivalent before.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    options.SlidingExpiration = true;
});

// Every page requires an authenticated user by default — the legacy app had no
// app-level authorization at all (Windows-trust model); this closes that gap.
// Course/Employee admin actions additionally require the "Admin" role (human
// decision, 2026-09-19) — see [Authorize(Roles = "Admin")] on the Edit/AssignCourse
// PageModels and the per-handler checks in the Index delete handlers.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Self-registration is admin-provisioned/invite-only (human decision, 2026-09-19) —
// enforced by overriding the Identity UI's Register page (Areas/Identity/Pages/
// Account/Register.cshtml) with our own [Authorize(Roles = "Admin")] version. A
// RazorPagesOptions.Conventions.AuthorizeAreaPage on the RCL's own page doesn't
// work here: that page is [AllowAnonymous] by design, and AllowAnonymous always
// wins over any Authorize requirement layered on top via a convention.
builder.Services.AddRazorPages();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Apply pending EF Core migrations on startup (small app, single instance —
    // revisit with a separate migration step if this ever needs multi-instance
    // rollout). Skipped under the test host (see Etms.Tests/EtmsWebApplicationFactory),
    // which swaps in an EF Core InMemory provider that doesn't support relational
    // migrations — role/bootstrap-admin seeding below still runs under the test host.
    if (!builder.Configuration.GetValue<bool>("UseInMemoryDatabaseForTests"))
    {
        services.GetRequiredService<EtmsDbContext>().Database.Migrate();
    }

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // Bootstraps the very first Admin account so someone can sign in at all once
    // self-registration is locked down. Only runs if that account doesn't exist yet;
    // the password must be rotated after first login (see README).
    var bootstrapEmail = builder.Configuration["Bootstrap:AdminEmail"];
    var bootstrapPassword = builder.Configuration["Bootstrap:AdminPassword"];
    if (!string.IsNullOrWhiteSpace(bootstrapEmail) && !string.IsNullOrWhiteSpace(bootstrapPassword))
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(bootstrapEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = bootstrapEmail,
                Email = bootstrapEmail,
                EmailConfirmed = true,
                DisplayName = "Bootstrap Admin",
            };
            var createResult = await userManager.CreateAsync(admin, bootstrapPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}

// Configure the HTTP request pipeline.
// ForwardedHeaders must run before anything that inspects scheme/remote IP
// (HSTS, HTTPS redirection, auth), otherwise it sees the proxy's connection
// instead of the original client's.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAuditLogging();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// Exposes the top-level-statement Program type to WebApplicationFactory<Program> in tests.
public partial class Program;
