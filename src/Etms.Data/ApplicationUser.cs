using Microsoft.AspNetCore.Identity;

namespace Etms.Data;

// DB-backed credential identity (replaces the legacy app's Windows Integrated Auth —
// see Architecture: ETMS .NET Modernization, KAN-16).
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
