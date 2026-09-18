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
var connectionString = builder.Configuration.GetConnectionString("Etms")
    ?? throw new InvalidOperationException("Connection string 'Etms' not configured.");

builder.Services.AddDbContext<EtmsDbContext>(options =>
    options.UseSqlServer(connectionString));

// Kestrel: don't disclose the server/framework via the Server response header (OWASP A05).
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Trust X-Forwarded-* headers from a reverse proxy doing TLS termination in front of
// this container (KAN-12 TLS requirement). If Kestrel terminates TLS directly instead
// (certificate configured via Kestrel:Certificates:Default), this is a harmless no-op.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
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
// and strict SameSite to reduce CSRF/session-hijacking exposure.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Every page requires an authenticated user by default — the legacy app had no
// app-level authorization at all (Windows-trust model); this closes that gap.
// NOTE: role-based least-privilege (e.g. restricting Course/Employee admin actions
// to an "Admin" role) is intentionally NOT enforced yet — the legacy app had no
// role distinction either, and deciding who should hold which role is a policy
// call for the human Engineering Lead/PO, not this agent. The "Admin" role is
// seeded below so that decision can be wired in without a further migration.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddRazorPages();

var app = builder.Build();

// Apply pending EF Core migrations on startup (small app, single instance —
// revisit with a separate migration step if this ever needs multi-instance rollout).
// Skipped under the test host (see Etms.Tests/EtmsWebApplicationFactory), which
// swaps in an EF Core InMemory provider that doesn't support relational migrations.
if (!builder.Configuration.GetValue<bool>("SkipMigrationOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    services.GetRequiredService<EtmsDbContext>().Database.Migrate();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders();
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
