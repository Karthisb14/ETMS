-- Data migration: legacy Frog.mdf (ADO.NET/stored-procedure schema) -> new Etms schema (EF Core).
-- Run once during cutover (KAN-11), after the new database has been migrated (`dotnet ef database update`
-- or the app's own startup auto-migrate) and before retiring the legacy app.
--
-- Assumes both databases are reachable from the same SQL Server instance (or linked server) at
-- migration time. Adjust the [Frog]. prefix to a linked-server-qualified name if they are not.
-- Additive/idempotent: safe to re-run, skips rows that already exist by natural/primary key.

SET IDENTITY_INSERT dbo.Course ON;

INSERT INTO dbo.Course (CourseId, Code, Name, Description)
SELECT src.CourseID, src.Code, src.Course, src.Description
FROM [Frog].dbo.Course AS src
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Course AS dst WHERE dst.CourseId = src.CourseID
);

SET IDENTITY_INSERT dbo.Course OFF;

SET IDENTITY_INSERT dbo.Employee ON;

INSERT INTO dbo.Employee (EmployeeId, Name, HireDate)
SELECT src.EmployeeID, src.Name, src.HireDate
FROM [Frog].dbo.Employee AS src
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Employee AS dst WHERE dst.EmployeeId = src.EmployeeID
);

SET IDENTITY_INSERT dbo.Employee OFF;

SET IDENTITY_INSERT dbo.EmployeeCourse ON;

INSERT INTO dbo.EmployeeCourse (EmployeeCourseId, EmployeeId, CourseId, IsPass, Note)
SELECT src.EmployeeCourseID, src.EmployeeID, src.CourseID, src.isPass, src.Note
FROM [Frog].dbo.EmployeeCourse AS src
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.EmployeeCourse AS dst WHERE dst.EmployeeCourseId = src.EmployeeCourseID
);

SET IDENTITY_INSERT dbo.EmployeeCourse OFF;

-- Verification: row counts should match between source and destination.
SELECT 'Course' AS TableName, (SELECT COUNT(*) FROM [Frog].dbo.Course) AS SourceCount, (SELECT COUNT(*) FROM dbo.Course) AS DestCount
UNION ALL
SELECT 'Employee', (SELECT COUNT(*) FROM [Frog].dbo.Employee), (SELECT COUNT(*) FROM dbo.Employee)
UNION ALL
SELECT 'EmployeeCourse', (SELECT COUNT(*) FROM [Frog].dbo.EmployeeCourse), (SELECT COUNT(*) FROM dbo.EmployeeCourse);
