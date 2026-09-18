using System.ComponentModel.DataAnnotations;
using Etms.Domain;
using Xunit;

namespace Etms.Tests;

public class EmployeeValidationTests
{
    [Fact]
    public void Employee_with_name_and_hire_date_is_valid()
    {
        var employee = new Employee { Name = "Jane Doe", HireDate = DateTime.Today };

        var results = Validate(employee);

        Assert.Empty(results);
    }

    [Fact]
    public void Employee_without_name_is_invalid()
    {
        var employee = new Employee { Name = "", HireDate = DateTime.Today };

        var results = Validate(employee);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Employee.Name)));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}
