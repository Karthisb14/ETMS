using Etms.Data;
using Etms.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Etms.Web.Pages.Courses;

// Course create/edit is Admin-only (human decision, 2026-09-19) — the legacy app had
// no such restriction, but self-registration is now invite-only and admin actions
// need to be reserved for the Admin role to match.
[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly EtmsDbContext _db;

    public EditModel(EtmsDbContext db) => _db = db;

    [BindProperty]
    public Course Course { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            Course = new Course();
            return Page();
        }

        var existing = await _db.Courses.FindAsync(id.Value);
        if (existing is null)
        {
            return NotFound();
        }

        Course = existing;
        return Page();
    }

    // Preserves the legacy Course_Insert/Course_Update business rules
    // (App_Code/CourseData.vb).
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Course.CourseId == 0)
        {
            _db.Courses.Add(Course);
        }
        else
        {
            _db.Courses.Update(Course);
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("./Index");
    }
}
