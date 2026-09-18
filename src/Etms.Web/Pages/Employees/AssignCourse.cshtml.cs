using Etms.Data;
using Etms.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Etms.Web.Pages.Employees;

public class AssignCourseModel : PageModel
{
    private readonly EtmsDbContext _db;

    public AssignCourseModel(EtmsDbContext db) => _db = db;

    [BindProperty(SupportsGet = true)]
    public int EmployeeId { get; set; }

    [BindProperty]
    public EmployeeCourse Assignment { get; set; } = new();

    public List<SelectListItem> CourseOptions { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int? employeeCourseId)
    {
        var employeeExists = await _db.Employees.AnyAsync(e => e.EmployeeId == EmployeeId);
        if (!employeeExists)
        {
            return NotFound();
        }

        if (employeeCourseId is not null)
        {
            var existing = await _db.EmployeeCourses.FindAsync(employeeCourseId.Value);
            if (existing is null || existing.EmployeeId != EmployeeId)
            {
                return NotFound();
            }

            Assignment = existing;
        }
        else
        {
            Assignment = new EmployeeCourse { EmployeeId = EmployeeId };
        }

        await LoadCourseOptionsAsync();
        return Page();
    }

    // Preserves the legacy EmployeeCourse_Insert/EmployeeCourse_Update business rules
    // (App_Code/EmployeeCourseData.vb).
    public async Task<IActionResult> OnPostAsync()
    {
        Assignment.EmployeeId = EmployeeId;

        if (!ModelState.IsValid)
        {
            await LoadCourseOptionsAsync();
            return Page();
        }

        if (Assignment.EmployeeCourseId == 0)
        {
            _db.EmployeeCourses.Add(Assignment);
        }
        else
        {
            _db.EmployeeCourses.Update(Assignment);
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("./Edit", new { id = EmployeeId });
    }

    private async Task LoadCourseOptionsAsync()
    {
        var courses = await _db.Courses.AsNoTracking().OrderBy(c => c.Code).ToListAsync();
        CourseOptions = courses
            .Select(c => new SelectListItem($"{c.Code} : {c.Name}", c.CourseId.ToString()))
            .ToList();
    }
}
