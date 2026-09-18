using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Etms.Domain;

// Mirrors legacy Forg.ETMS.EmployeeCourse (App_Code/EmployeeCourse.vb) — the Employee<->Course
// assignment, including pass/fail status and a free-text note.
public class EmployeeCourse
{
    public int EmployeeCourseId { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int CourseId { get; set; }
    public Course? Course { get; set; }

    public bool IsPass { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    [NotMapped]
    public string CourseLabel => Course is null ? string.Empty : $"{Course.Code} : {Course.Name}";
}
