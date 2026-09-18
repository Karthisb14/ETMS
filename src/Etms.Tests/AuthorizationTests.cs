using System.Net;
using Xunit;

namespace Etms.Tests;

// Verifies the KAN-16/KAN-12 fix: every page requires authentication, unlike the
// legacy Web Forms app (Windows-trust model, no app-level authorization at all).
public class AuthorizationTests : IClassFixture<EtmsWebApplicationFactory>
{
    private readonly EtmsWebApplicationFactory _factory;

    public AuthorizationTests(EtmsWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/Courses/Index")]
    [InlineData("/Employees/Index")]
    [InlineData("/Courses/Edit")]
    [InlineData("/Employees/Edit")]
    public async Task Anonymous_request_to_protected_page_redirects_to_login(string path)
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Identity/Account/Login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Login_page_itself_is_reachable_anonymously()
    {
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Identity/Account/Login");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
    }
}
