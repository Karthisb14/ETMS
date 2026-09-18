using Etms.Data;
using Etms.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Etms.Web.Pages.Courses;

public class IndexModel : PageModel
{
    private readonly EtmsDbContext _db;

    public IndexModel(EtmsDbContext db) => _db = db;

    public List<Course> Courses { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Courses = await _db.Courses.AsNoTracking().OrderBy(c => c.Code).ToListAsync();
    }

    // Preserves the legacy Course_Delete business rule (App_Code/CourseData.vb).
    // Delete is Admin-only; the page itself (listing) stays open to any authenticated
    // user, so this is checked per-handler rather than with a page-level [Authorize].
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var course = await _db.Courses.FindAsync(id);
        if (course is not null)
        {
            _db.Courses.Remove(course);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
