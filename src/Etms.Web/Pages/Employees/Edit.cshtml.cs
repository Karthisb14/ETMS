using Etms.Data;
using Etms.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Etms.Web.Pages.Employees;

public class EditModel : PageModel
{
    private readonly EtmsDbContext _db;

    public EditModel(EtmsDbContext db) => _db = db;

    [BindProperty]
    public Employee Employee { get; set; } = new();

    public List<EmployeeCourse> Assignments { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            Employee = new Employee { HireDate = DateTime.Today };
            return Page();
        }

        var existing = await _db.Employees.FindAsync(id.Value);
        if (existing is null)
        {
            return NotFound();
        }

        Employee = existing;
        Assignments = await _db.EmployeeCourses
            .AsNoTracking()
            .Include(ec => ec.Course)
            .Where(ec => ec.EmployeeId == id.Value)
            .OrderBy(ec => ec.Course!.Code)
            .ToListAsync();

        return Page();
    }

    // Preserves the legacy Employee_Insert/Employee_Update business rules
    // (App_Code/EmployeeData.vb).
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Employee.EmployeeId == 0)
        {
            _db.Employees.Add(Employee);
        }
        else
        {
            _db.Employees.Update(Employee);
        }

        await _db.SaveChangesAsync();
        return RedirectToPage("./Edit", new { id = Employee.EmployeeId });
    }
}
