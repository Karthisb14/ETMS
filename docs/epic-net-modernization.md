# Epic: Modernize ETMS to Latest .NET

Status: **PO_APPROVED**.
Publish mode: **live** — the Atlassian MCP connection was established and the
artifacts below were published to Jira/Confluence. This file is now a local
mirror, not the source of truth.

- Jira Epic: [KAN-10](https://karthikeyanvjkk.atlassian.net/browse/KAN-10) — status **In Progress** (transitioned from To Do to reflect PO_APPROVED)
- Confluence requirements page: [Requirements: ETMS .NET Modernization](https://karthikeyanvjkk.atlassian.net/wiki/spaces/KAN/pages/622593/Requirements+ETMS+.NET+Modernization)
- Confluence NFR spec: [NFR Spec: ETMS .NET Modernization](https://karthikeyanvjkk.atlassian.net/wiki/spaces/KAN/pages/589825/NFR+Spec+ETMS+.NET+Modernization)

> **Human PO approval recorded 2026-09-19** via chat (`po_approve`). The
> AI-DLC `workflow_approve` / `workflow_apply` transition tools are still not
> connected in this session (only the Atlassian MCP is), so the state
> transition itself is still a manual note rather than a replayed AI-DLC
> event — but the artifacts themselves are now live in Jira/Confluence. The
> open risks below (2-day timeline, unspecified NFR targets, HIPAA scope) are
> approved **as open follow-ups**, not as resolved — they still need answers
> before Engineering Review can finalize a technical plan.

## Problem

ETMS (Employee Training Management System) runs on ASP.NET Web Forms targeting
**.NET Framework 3.5** (VB.NET code-behind, `System.Web.UI.Page`, LINQ-to-SQL
`.dbml` data access, `System.Data.SqlClient`). This stack is out of support,
hard to host outside Windows/IIS, and increasingly hard to staff. The business
wants to move to the latest supported .NET so the app remains secure,
supportable, and able to evolve.

Web Forms has no equivalent in modern .NET (5+) — there is no supported
in-place upgrade path. Modernizing means re-platforming the UI (e.g. to
Blazor or ASP.NET Core MVC/Razor Pages) and replacing LINQ-to-SQL with a
supported data-access layer (e.g. EF Core), while carrying forward the
existing business logic and data.

## Target users

Internal staff who use ETMS today: employees, training administrators/admins,
and course administrators (based on existing `EmployeeAdmin`, `CourseAdmin`,
`EmployeeListing`, `CourseListing` pages).

## Goals / non-goals

- Goal: Move ETMS off .NET Framework 3.5 / Web Forms onto a current, supported
  .NET release, eliminating the framework end-of-life/security risk.
- Goal: Preserve all existing employee/course/training-record functionality
  across the migration.
- Goal: Improve deployability/hosting flexibility and developer velocity
  (modern, more available skill set than VB.NET Web Forms).
- Non-goal (for this epic): New business features unrelated to the
  migration. UX/workflow improvements are allowed opportunistically during
  the rewrite (see Business rules) but are not the primary goal.
- Non-goal: Migrating other repositories in this workspace (MERN frontend,
  Spring PetClinic) — out of scope; ETMS only.

## Business rules

1. All pages currently in ETMS (`Default`, `CourseListing`, `CourseAdmin`,
   `EmployeeListing`, `EmployeeAdmin`, and their supporting master
   page/controls) must have an equivalent screen in the modernized app.
2. The business has explicitly approved taking the opportunity to improve
   UX/workflows during the rewrite — strict pixel/workflow parity is **not**
   required, but no existing capability may be dropped without an explicit
   decision recorded against this epic.
3. Employee PII (names, training records) and any PHI-adjacent data must be
   protected at the same or better level than today, per compliance
   requirements below.
4. Target UI framework and migration approach (big-bang vs. incremental/
   strangler) are technical decisions delegated to the Engineering Lead
   Agent — not decided by this epic.

## Acceptance criteria

1. Given an employee/admin user, when they perform any action currently
   supported (view/add/edit employees, view/add/edit courses, view course
   and employee listings), then the modernized app supports the same
   capability without data loss.
2. Given the modernized app is deployed, when checked, it targets a current,
   in-support .NET release (not .NET Framework) with no Web Forms
   dependency.
3. Given existing data in the current SQL Server database (`Frog.mdf`),
   when the modernized app reads/writes it, then data access uses a
   supported, non-EOL technology (e.g. EF Core) and existing data is
   migrated without loss.
4. Given the app handles employee PII and PHI-adjacent data, when audited,
   then access controls, transport encryption, and data-handling meet HIPAA
   and internal PII-handling obligations (see NFRs).
5. Given the migration is complete, when the old Web Forms app is retired,
   then there is a rollback or parallel-run plan agreed with Engineering
   Lead (approach TBD — see Business rule 4).

## Non-functional requirements

| Category     | Requirement                                                | Target / Notes                                                                                             |
| ------------ | ---------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Security     | Framework must be in active vendor support                 | Latest current .NET release, no components past EOL                                                        |
| Compliance   | HIPAA safeguards for PHI-adjacent data                     | Access controls, encryption in transit/at rest, audit logging — Engineering Lead to confirm implementation |
| Compliance   | PII protection for employee data (names, training records) | Least-privilege access, encryption in transit/at rest                                                      |
| Availability | Not specified by PO                                        | **Not applicable — no target given; mark for Engineering Lead follow-up if this app is business-critical** |
| Performance  | Not specified by PO                                        | **Not applicable — no target given**                                                                       |
| Scalability  | Called out as a driver, no explicit target given           | Engineering Lead to propose target based on current usage                                                  |
| Reliability  | Not specified by PO                                        | **Not applicable — no target given**                                                                       |

## Supporting documents / design references

- None supplied yet. Existing code is the primary reference:
  [ETMS/App_Code](../App_Code), [ETMS/web.config](../web.config),
  [ETMS/Frog.master](../Frog.master).

## Duration / target

- Stated urgency: vendor/framework EOL is the driver, "2 days" was given as
  the target timeframe.
- **Flagged risk, not accepted as-is:** a full re-platform off Web Forms
  (UI framework replacement + data-access layer replacement + compliance
  validation) cannot be delivered in 2 days. This needs the human PO and
  Engineering Lead to agree on a realistic timeline/phasing before
  `po_approve`. Recorded here so it isn't silently dropped.

## Open questions (for human PO / Engineering Lead)

1. What is the actual EOL/contract/audit date driving urgency, and is a
   phased migration (incremental page-by-page) acceptable to meet it, even
   if full cutover takes longer?
2. Is ETMS business-critical enough to need explicit availability/
   performance SLAs, or is "at least as good as today" sufficient?
3. Confirm HIPAA applicability precisely — does ETMS store or merely
   reference PHI, and is a formal compliance review required before
   go-live?
4. Any hosting constraints (must stay on-prem/Windows, or is cloud/Linux
   hosting acceptable) that affect the Engineering Lead's technical
   recommendation?

## Next step

Approved by human PO on 2026-09-19. Ready to hand off to the **Engineering
Lead Agent** for Engineering Review / technical planning (`.github/chatmodes/engineering-lead.chatmode.md`),
which owns: target UI framework choice, migration strategy (big-bang vs.
incremental), and a realistic timeline given the 2-day target is
infeasible for the scope described above.

## Engineering Review (2026-09-19)

Status: **draft**, pending human Engineering Lead approval (`eng_approve`).

- Architecture: [Architecture: ETMS .NET Modernization](https://karthikeyanvjkk.atlassian.net/wiki/spaces/KAN/pages/720897/Architecture+ETMS+.NET+Modernization)
- Technical Design: [Technical Design: ETMS .NET Modernization](https://karthikeyanvjkk.atlassian.net/wiki/spaces/KAN/pages/557058/Technical+Design+ETMS+.NET+Modernization)
- Child stories (corrected mapping after an initial mix-up): KAN-18
  (Foundation), KAN-17 (Data layer), KAN-16 (Auth), KAN-15 (Course pages),
  KAN-14 (Employee pages), KAN-13 (EmployeeCourse UI), KAN-12 (Compliance
  hardening), KAN-11 (Parallel-run + cutover)

Decisions made this session (human-confirmed): .NET 10 + ASP.NET Core
MVC/Razor Pages + EF Core + C#; Linux containers; full rewrite (not
incremental); auth moves from Windows Integrated Auth to DB-backed
credentials via ASP.NET Core Identity.

## Open questions resolved (2026-09-19)

1. Container platform: plain Docker Compose.
2. No existing org SSO/identity-provider requirement — DB-backed Identity stands.
3. Real deadline confirmed: **3 days**.
4. HIPAA applies — compliance hardening (KAN-12) is a hard requirement.
5. `EmployeeListing_24JAN09.aspx` confirmed dead code, excluded from migration.

**Flagged, not silently accepted:** 3 days is not enough calendar time for
the full scope (foundation, data layer, auth, 3 sets of CRUD pages,
HIPAA-grade compliance hardening, parallel-run validation, human review
gates), even with AI-agent-assisted development. Recorded as an open
decision for the human Engineering Lead: either (A) cut scope to a
non-compliant MVP in 3 days with hardening/employee pages/cutover deferred,
or (B) treat 3 days as the foundation/data/auth milestone only, with full
delivery taking longer. Not resolved yet — needs explicit sign-off before
`READY_FOR_DEVELOPMENT`, since acceptance criterion 4 (HIPAA/PII) cannot be
waived silently.

## eng_approve (2026-09-19)

Human Engineering Lead approved with **Option B: full functionality
required, scope not cut**. All 8 stories (KAN-11..KAN-18) remain in scope,
including HIPAA compliance hardening (KAN-12) and parallel-run validation
(KAN-11). The 3-day figure does not override completeness/compliance —
timeline extends as needed. Epic status: **ENG_APPROVED**. Next per
manifest routing: `READY_FOR_DEVELOPMENT` → Developer Agent.

## Development progress (2026-09-19)

Branch `feat/kan-10-etms-net-modernization`, commit `3ea4e99`, code under
[ETMS/src](../src). Implemented and marked Done in Jira: KAN-18
(Foundation), KAN-17 (Data layer), KAN-16 (Auth), KAN-15 (Course pages),
KAN-14 (Employee pages), KAN-13 (EmployeeCourse assignment UI). `dotnet
build`/`dotnet test` pass (10/10); `docker compose config` validates.

Still open: KAN-12 (Compliance hardening — only groundwork done: security
headers, audit-log middleware, password policy; full TLS/secrets-manager/
OWASP review outstanding) and KAN-11 (Parallel-run + cutover — not
started). Container build not verified against a running Docker daemon.
Self-registration policy (open vs. admin-provisioned) is an explicit open
item for KAN-12. See [ETMS/src/README.md](../src/README.md) for full
"Open items" list.

## KAN-12 compliance hardening progress (2026-09-19)

Commit `9325ddc`. Added: durable `AuditLogEntry` table (queryable),
HttpOnly/Secure/SameSite=Strict auth cookie, Kestrel Server-header
suppression, `ForwardedHeaders` for reverse-proxy TLS termination, Data
Protection key persistence via mounted volume, and integration tests
proving every page requires auth (15/15 tests passing — caught and fixed
a real missing-`_LoginPartial` bug).

Still open, flagged rather than assumed: role-based least privilege
(Admin role seeded, not yet enforced on any page — needs a human policy
decision on who holds it), TLS certificate provisioning, encryption at
rest for the DB volume, self-registration policy, and CI wiring
(governance surface, outside this agent's grant). KAN-12 kept at **In
Progress**, not Done, until those close.

## KAN-12 resolved and closed (2026-09-19)

Commit `3ec0114`. Human decisions obtained and implemented, 26/26 tests
passing:

1. Role enforcement: Course/Employee add/edit/delete require "Admin" role;
   listings stay open to any authenticated user.
2. Self-registration: locked to admin-provisioned/invite-only via an
   override of Identity UI's `[AllowAnonymous]` Register page (a
   convention alone can't win against `AllowAnonymous`); bootstrap admin
   seeded from config to avoid a chicken-and-egg problem.
3. TLS: reverse proxy/load balancer terminates TLS; `ForwardedHeaders`
   moved to the front of the pipeline.
4. Encryption at rest: confirmed already handled by infrastructure.

Two real bugs found and fixed: a Razor parser ambiguity (bare text after a
tag-helper `<a>` inside a nested `@if`) and an EF Core dual-provider
registration conflict in the test host (fixed via env-var-driven provider
selection in `Program.cs`). KAN-12 marked **Done**.

## KAN-11 progress (2026-09-19)

Commit `0215f64`. Added `DataMigration/cutover-runbook.md` (pre-cutover
checklist, data migration steps, parallel-run procedure mapped to every
KAN-10 acceptance criterion, cutover, rollback, retirement) and
PageModel-level acceptance tests for the full Course/Employee/
EmployeeCourse CRUD flows (23/23 tests passing). Actual parallel run,
staging deployment, real-user UAT, and cutover require a real environment
and human sign-off — not something this agent can execute. KAN-11 kept at
**In Progress**, blocked on KAN-12 closing fully and a staging environment
existing.
