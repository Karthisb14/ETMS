using System.Net;
using Etms.Data;
using Etms.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Etms.Tests;

// Human decision (2026-09-19): Course/Employee admin actions require the "Admin" role;
// listing pages stay open to any authenticated user. Self-registration is locked down
// to Admin-only. Verifies both, plus the bootstrap-admin seeding path used to escape
// the resulting chicken-and-egg problem (no Admin exists yet on first run).
public class RoleEnforcementTests : IClassFixture<EtmsWebApplicationFactory>
{
    private readonly EtmsWebApplicationFactory _factory;

    public RoleEnforcementTests(EtmsWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_request_to_register_page_redirects_to_login()
    {
        // The Register page itself now requires an authenticated Admin, so an
        // anonymous visitor is redirected to log in first (not straight to a 403).
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Identity/Account/Register");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Identity/Account/Login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Seeded_admin_role_exists_after_startup()
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

        Assert.True(await roleManager.RoleExistsAsync("Admin"));
    }

    [Fact]
    public async Task Non_admin_user_cannot_delete_a_course()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EtmsDbContext>();
        db.Courses.Add(new Course { Code = "C1", Name = "Course One" });
        await db.SaveChangesAsync();

        // Directly exercises the per-handler role check (see Courses/Index.cshtml.cs)
        // since simulating a full non-admin authenticated HTTP request needs a signed
        // cookie the in-memory test host doesn't issue without a real sign-in flow.
        var pageModel = new Etms.Web.Pages.Courses.IndexModel(db)
        {
            PageContext = TestPageContext.ForRole("Employee"),
        };

        var result = await pageModel.OnPostDeleteAsync(db.Courses.Single().CourseId);

        Assert.IsType<Microsoft.AspNetCore.Mvc.ForbidResult>(result);
        Assert.Single(await db.Courses.ToListAsync());
    }
}
