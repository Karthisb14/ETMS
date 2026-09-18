using Etms.Data;
using Etms.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Etms.Web.Pages.Employees;

public class IndexModel : PageModel
{
    private readonly EtmsDbContext _db;

    public IndexModel(EtmsDbContext db) => _db = db;

    public List<Employee> Employees { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Employees = await _db.Employees.AsNoTracking().OrderBy(e => e.Name).ToListAsync();
    }

    // Preserves the legacy Employee_Delete business rule (App_Code/EmployeeData.vb).
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee is not null)
        {
            _db.Employees.Remove(employee);
            await _db.SaveChangesAsync();
        }

        return RedirectToPage();
    }
}
