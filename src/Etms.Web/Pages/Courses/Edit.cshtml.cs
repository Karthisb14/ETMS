using Etms.Data;
using Etms.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Etms.Web.Pages.Courses;

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
