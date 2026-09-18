using Etms.Data;
using Etms.Web.Pages.Employees;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Etms.Tests;

// KAN-10 acceptance criterion 1 for the Employee + EmployeeCourse assignment flow —
// the direct replacement for the legacy EmployeeAdmin.aspx + EmployeeCourse.ascx.
public class EmployeesPageModelTests
{
    private static EtmsDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<EtmsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new EtmsDbContext(options);
    }

    [Fact]
    public async Task Add_employee_then_list_shows_it()
    {
        await using var db = CreateContext(nameof(Add_employee_then_list_shows_it));

        var editModel = new EditModel(db)
        {
            Employee = new() { Name = "Jane Doe", HireDate = new DateTime(2024, 1, 15) },
        };
        await editModel.OnPostAsync();

        var indexModel = new IndexModel(db);
        await indexModel.OnGetAsync();

        var employee = Assert.Single(indexModel.Employees);
        Assert.Equal("Jane Doe", employee.Name);
    }

    [Fact]
    public async Task Delete_employee_removes_it_from_listing()
    {
        await using var db = CreateContext(nameof(Delete_employee_removes_it_from_listing));
        db.Employees.Add(new() { Name = "Jane Doe", HireDate = DateTime.Today });
        await db.SaveChangesAsync();
        var employeeId = db.Employees.Single().EmployeeId;

        var indexModel = new IndexModel(db);
        await indexModel.OnPostDeleteAsync(employeeId);

        Assert.Empty(await db.Employees.ToListAsync());
    }

    [Fact]
    public async Task Assign_course_to_employee_then_edit_page_shows_assignment()
    {
        await using var db = CreateContext(nameof(Assign_course_to_employee_then_edit_page_shows_assignment));
        db.Employees.Add(new() { Name = "Jane Doe", HireDate = DateTime.Today });
        db.Courses.Add(new() { Code = "C1", Name = "Course One" });
        await db.SaveChangesAsync();
        var employeeId = db.Employees.Single().EmployeeId;
        var courseId = db.Courses.Single().CourseId;

        var assignModel = new AssignCourseModel(db) { EmployeeId = employeeId };
        await assignModel.OnGetAsync(employeeCourseId: null);
        assignModel.Assignment.CourseId = courseId;
        assignModel.Assignment.IsPass = true;
        assignModel.Assignment.Note = "Completed with distinction";
        var result = await assignModel.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);

        var editModel = new EditModel(db);
        await editModel.OnGetAsync(employeeId);

        var assignment = Assert.Single(editModel.Assignments);
        Assert.True(assignment.IsPass);
        Assert.Equal("Completed with distinction", assignment.Note);
        Assert.Equal("C1 : Course One", assignment.CourseLabel);
    }

    [Fact]
    public async Task Editing_existing_assignment_updates_pass_status()
    {
        await using var db = CreateContext(nameof(Editing_existing_assignment_updates_pass_status));
        db.Employees.Add(new() { Name = "Jane Doe", HireDate = DateTime.Today });
        db.Courses.Add(new() { Code = "C1", Name = "Course One" });
        await db.SaveChangesAsync();
        var employeeId = db.Employees.Single().EmployeeId;
        var courseId = db.Courses.Single().CourseId;

        db.EmployeeCourses.Add(new() { EmployeeId = employeeId, CourseId = courseId, IsPass = false });
        await db.SaveChangesAsync();
        var employeeCourseId = db.EmployeeCourses.Single().EmployeeCourseId;

        var assignModel = new AssignCourseModel(db) { EmployeeId = employeeId };
        await assignModel.OnGetAsync(employeeCourseId);
        assignModel.Assignment.IsPass = true;
        await assignModel.OnPostAsync();

        var reloaded = await db.EmployeeCourses.FindAsync(employeeCourseId);
        Assert.True(reloaded!.IsPass);
    }
}
