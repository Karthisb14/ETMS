using System.ComponentModel.DataAnnotations;
using Etms.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Etms.Web.Areas.Identity.Pages.Account;

// Overrides the Identity UI RCL's default Register page, which is [AllowAnonymous]
// by design — that can't be fixed with an authorization convention because
// AllowAnonymous always wins over any Authorize requirement layered on top. This
// app-level page (same route) takes precedence over the RCL one and is Admin-only,
// per the human decision (2026-09-19) to make account creation invite-only.
[Authorize(Roles = "Admin")]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterModel(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            EmailConfirmed = true,
            DisplayName = Input.DisplayName,
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        if (Input.IsAdmin)
        {
            await _userManager.AddToRoleAsync(user, "Admin");
        }

        return RedirectToPage("/Index", new { area = "" });
    }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }
    }
}
