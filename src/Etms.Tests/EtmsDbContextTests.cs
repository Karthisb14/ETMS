using Etms.Data;
using Etms.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Etms.Tests;

// Integration-style tests against EF Core InMemory, covering the CRUD behavior that
// used to live in the legacy CourseData/EmployeeData/EmployeeCourseData stored-procedure
// wrappers. Swap to Testcontainers + real SQL Server before relying on these for
// SQL-Server-specific behavior (identity columns, unique index enforcement, etc.).
public class EtmsDbContextTests
{
    private static EtmsDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<EtmsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new EtmsDbContext(options);
    }

    [Fact]
    public async Task Course_insert_then_select_all_returns_it()
    {
        await using var db = CreateContext(nameof(Course_insert_then_select_all_returns_it));

        db.Courses.Add(new Course { Code = "C1", Name = "Course One" });
        await db.SaveChangesAsync();

        var all = await db.Courses.ToListAsync();

        Assert.Single(all);
        Assert.Equal("C1", all[0].Code);
    }

    [Fact]
    public async Task Course_update_persists_changes()
    {
        await using var db = CreateContext(nameof(Course_update_persists_changes));
        var course = new Course { Code = "C1", Name = "Original" };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        course.Name = "Updated";
        await db.SaveChangesAsync();

        var reloaded = await db.Courses.FindAsync(course.CourseId);
        Assert.Equal("Updated", reloaded!.Name);
    }

    [Fact]
    public async Task Course_delete_removes_it()
    {
        await using var db = CreateContext(nameof(Course_delete_removes_it));
        var course = new Course { Code = "C1", Name = "Course One" };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        db.Courses.Remove(course);
        await db.SaveChangesAsync();

        Assert.Empty(await db.Courses.ToListAsync());
    }

    [Fact]
    public async Task Employee_insert_creates_employee_without_courses()
    {
        await using var db = CreateContext(nameof(Employee_insert_creates_employee_without_courses));

        db.Employees.Add(new Employee { Name = "Jane Doe", HireDate = DateTime.Today });
        await db.SaveChangesAsync();

        var employee = Assert.Single(await db.Employees.ToListAsync());
        Assert.Equal("Jane Doe", employee.Name);
    }

    [Fact]
    public async Task EmployeeCourse_assignment_links_employee_and_course()
    {
        await using var db = CreateContext(nameof(EmployeeCourse_assignment_links_employee_and_course));

        var employee = new Employee { Name = "Jane Doe", HireDate = DateTime.Today };
        var course = new Course { Code = "C1", Name = "Course One" };
        db.Employees.Add(employee);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        db.EmployeeCourses.Add(new EmployeeCourse
        {
            EmployeeId = employee.EmployeeId,
            CourseId = course.CourseId,
            IsPass = true,
            Note = "Completed on time",
        });
        await db.SaveChangesAsync();

        var assignment = Assert.Single(
            await db.EmployeeCourses.Include(ec => ec.Course).ToListAsync());
        Assert.True(assignment.IsPass);
        Assert.Equal("C1 : Course One", assignment.CourseLabel);
    }
}
