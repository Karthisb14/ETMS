# Cutover Runbook: ETMS .NET Modernization (KAN-11)

Linked epic: KAN-10. This is the parallel-run / validation / cutover plan
required by KAN-10 acceptance criterion 5 ("a rollback / parallel-run plan
is agreed before the legacy app is retired").

## 1. Pre-cutover checklist

- [ ] KAN-12 compliance hardening fully closed (TLS certificate provisioned,
      encryption at rest confirmed, role-enforcement policy decided,
      self-registration policy decided) — see `../README.md` open items.
- [ ] `dotnet test` passes (currently 23/23 — auth enforcement, CRUD parity,
      EF Core data layer).
- [ ] Container image build verified against a running Docker daemon
      (not yet done in this environment — see README).
- [ ] New SQL Server instance provisioned and reachable from the new app's
      container/host.
- [ ] New app's database schema created (`dotnet ef database update`, or the
      app's own startup auto-migration).

## 2. Data migration

1. Take a backup of the legacy `Frog.mdf` database.
2. Run `../DataMigration/migrate-legacy-data.sql` against the new database
   (script is additive/idempotent — safe to re-run).
3. Confirm the verification query at the end of that script reports matching
   row counts for `Course`, `Employee`, and `EmployeeCourse` between source
   and destination.
4. Spot-check a sample of migrated records in the new app's UI (Courses and
   Employees listings) against the legacy app's listings for the same data.

## 3. Parallel run

1. Deploy the new app to a staging/parallel environment pointed at the
   migrated database copy (not production data yet).
2. Provision test accounts for each user role that will use the app
   (employees, training admins, course admins — see the still-open
   role-enforcement decision in KAN-12).
3. Have representative users from each role exercise every acceptance
   criterion in KAN-10 against the parallel environment:
   - View/add/edit/delete courses (`/Courses`)
   - View/add/edit/delete employees (`/Employees`)
   - Assign a course to an employee, mark pass/fail, add a note
     (`/Employees/{id}` → `Assign course`)
4. Record discrepancies against the legacy app's behavior. Per KAN-10
   business rule 2, UX/workflow improvements are acceptable — only *loss*
   of an existing capability is a blocker, not a difference in workflow.
5. Re-run the data migration script against a fresh copy of the legacy data
   close to the actual cutover date, so the parallel-run data isn't stale.

## 4. Cutover

1. Announce a maintenance window; legacy app becomes read-only (or offline)
   for its duration.
2. Run the final data migration pass (step 2 above) against the
   as-of-cutover legacy data.
3. Point production traffic (DNS/reverse proxy) at the new app.
4. Smoke-test the 5 core flows in production (list/add for Courses,
   Employees, and course assignment).
5. Keep the legacy app and its database available, untouched, for the
   rollback window below — do not decommission yet.

## 5. Rollback plan

If a blocking defect is found after cutover:

1. Point production traffic back at the legacy app (reverse DNS/proxy
   change from step 4.3).
2. Any data entered into the new app during the cutover window must be
   manually reconciled back into the legacy database before re-attempting
   cutover — the migration script is one-directional (legacy → new) and has
   no built-in reverse sync.
3. Treat the failed cutover attempt's root cause as a new defect against the
   KAN-10 epic before scheduling a second cutover attempt.

## 6. Retirement

Only after a rollback window (recommend at least one full business cycle —
e.g. a full payroll/training-reporting period, to be confirmed by the human
Engineering Lead/PO) has passed with no rollback triggered:

1. Take a final backup of the legacy `Frog.mdf` database and its application
   code, and archive it per the org's retention policy.
2. Decommission the legacy IIS site and its App Pool.
3. Close KAN-11 and KAN-10.
