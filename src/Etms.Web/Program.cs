using Etms.Data;
using Etms.Web.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Connection string comes from configuration (env var ConnectionStrings__Etms in
// docker-compose / secrets manager in production) — never hard-coded, unlike the
// legacy app's plaintext web.config connection string.
var connectionString = builder.Configuration.GetConnectionString("Etms")
    ?? throw new InvalidOperationException("Connection string 'Etms' not configured.");

builder.Services.AddDbContext<EtmsDbContext>(options =>
    options.UseSqlServer(connectionString));

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
    .AddEntityFrameworkStores<EtmsDbContext>();

// Every page requires an authenticated user by default — the legacy app had no
// app-level authorization at all (Windows-trust model); this closes that gap.
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
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<EtmsDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
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
