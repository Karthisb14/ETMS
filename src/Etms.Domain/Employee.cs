using System.ComponentModel.DataAnnotations;

namespace Etms.Domain;

// Mirrors legacy Forg.ETMS.Employee (App_Code/Employee.vb) — same fields, same semantics.
public class Employee
{
    public int EmployeeId { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime HireDate { get; set; }

    public List<EmployeeCourse> EmployeeCourses { get; set; } = new();
}
