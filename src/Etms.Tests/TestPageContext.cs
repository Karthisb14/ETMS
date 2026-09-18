using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Etms.Tests;

// Shared helper for PageModel-level tests that need a signed-in user's PageContext
// without going through a full HTTP + Identity sign-in flow.
internal static class TestPageContext
{
    public static PageContext ForRole(string role) => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, role)], authenticationType: "Test")),
        },
    };
}
