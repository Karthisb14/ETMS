using System.ComponentModel.DataAnnotations;

namespace Etms.Domain;

// Mirrors legacy Forg.ETMS.Course (App_Code/Course.vb) — same fields, same semantics.
public class Course
{
    public int CourseId { get; set; }

    [Required, StringLength(4)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public List<EmployeeCourse> EmployeeCourses { get; set; } = new();
}
