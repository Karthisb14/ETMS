using Etms.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Etms.Data;

public class EtmsDbContext : IdentityDbContext<ApplicationUser>
{
    public EtmsDbContext(DbContextOptions<EtmsDbContext> options) : base(options)
    {
    }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeCourse> EmployeeCourses => Set<EmployeeCourse>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AuditLogEntry>(entity =>
        {
            entity.ToTable("AuditLogEntry");
            entity.HasKey(a => a.AuditLogEntryId);
            entity.HasIndex(a => a.TimestampUtc);
        });

        builder.Entity<Course>(entity =>
        {
            entity.ToTable("Course");
            entity.HasKey(c => c.CourseId);
            entity.HasIndex(c => c.Code).IsUnique();
        });

        builder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employee");
            entity.HasKey(e => e.EmployeeId);
        });

        builder.Entity<EmployeeCourse>(entity =>
        {
            entity.ToTable("EmployeeCourse");
            entity.HasKey(ec => ec.EmployeeCourseId);

            entity.HasOne(ec => ec.Employee)
                .WithMany(e => e.EmployeeCourses)
                .HasForeignKey(ec => ec.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ec => ec.Course)
                .WithMany(c => c.EmployeeCourses)
                .HasForeignKey(ec => ec.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // One assignment per employee/course pair (matches legacy business rule).
            entity.HasIndex(ec => new { ec.EmployeeId, ec.CourseId }).IsUnique();
        });
    }
}
