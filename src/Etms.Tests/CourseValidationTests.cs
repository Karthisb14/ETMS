using System.ComponentModel.DataAnnotations;
using Etms.Domain;
using Xunit;

namespace Etms.Tests;

public class CourseValidationTests
{
    [Fact]
    public void Course_with_all_required_fields_is_valid()
    {
        var course = new Course { Code = "C101", Name = "Safety Basics" };

        var results = Validate(course);

        Assert.Empty(results);
    }

    [Fact]
    public void Course_without_code_is_invalid()
    {
        var course = new Course { Code = "", Name = "Safety Basics" };

        var results = Validate(course);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Course.Code)));
    }

    [Fact]
    public void Course_without_name_is_invalid()
    {
        var course = new Course { Code = "C101", Name = "" };

        var results = Validate(course);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Course.Name)));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}
