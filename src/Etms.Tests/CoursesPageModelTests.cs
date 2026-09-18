using Etms.Data;
using Etms.Web.Pages.Courses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Etms.Tests;

// Exercises the actual Razor Page handlers (not just the DbContext) to substantiate
// KAN-10 acceptance criterion 1: CRUD/listing works without data loss. Complements
// EtmsDbContextTests (data layer) and AuthorizationTests (every page requires login).
public class CoursesPageModelTests
{
    private static EtmsDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<EtmsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new EtmsDbContext(options);
    }

    [Fact]
    public async Task Add_course_then_list_shows_it()
    {
        await using var db = CreateContext(nameof(Add_course_then_list_shows_it));

        var editModel = new EditModel(db)
        {
            Course = new() { Code = "C1", Name = "Course One", Description = "Desc" },
        };
        var result = await editModel.OnPostAsync();
        Assert.IsType<RedirectToPageResult>(result);

        var indexModel = new IndexModel(db);
        await indexModel.OnGetAsync();

        var course = Assert.Single(indexModel.Courses);
        Assert.Equal("C1", course.Code);
        Assert.Equal("Course One", course.Name);
    }

    [Fact]
    public async Task Edit_course_persists_changes()
    {
        await using var db = CreateContext(nameof(Edit_course_persists_changes));
        db.Courses.Add(new() { Code = "C1", Name = "Original" });
        await db.SaveChangesAsync();
        var courseId = db.Courses.Single().CourseId;

        var editModel = new EditModel(db);
        await editModel.OnGetAsync(courseId);
        editModel.Course.Name = "Updated";
        await editModel.OnPostAsync();

        var reloaded = await db.Courses.FindAsync(courseId);
        Assert.Equal("Updated", reloaded!.Name);
    }

    [Fact]
    public async Task Invalid_course_is_not_saved()
    {
        await using var db = CreateContext(nameof(Invalid_course_is_not_saved));

        var editModel = new EditModel(db) { Course = new() { Code = "", Name = "" } };
        editModel.ModelState.AddModelError("Course.Code", "Required");

        var result = await editModel.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Empty(await db.Courses.ToListAsync());
    }

    [Fact]
    public async Task Delete_course_removes_it_from_listing()
    {
        await using var db = CreateContext(nameof(Delete_course_removes_it_from_listing));
        db.Courses.Add(new() { Code = "C1", Name = "Course One" });
        await db.SaveChangesAsync();
        var courseId = db.Courses.Single().CourseId;

        var indexModel = new IndexModel(db) { PageContext = TestPageContext.ForRole("Admin") };
        await indexModel.OnPostDeleteAsync(courseId);

        Assert.Empty(await db.Courses.ToListAsync());
    }
}
