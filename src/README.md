# ETMS (modernized)

ASP.NET Core (.NET 10) replacement for the legacy ASP.NET Web Forms ETMS app
in the repo root. See `../docs/epic-net-modernization.md` and the linked
Jira epic (KAN-10) / Confluence architecture & technical design pages for
the full plan.

## Projects

- `Etms.Web` — Razor Pages UI (Courses, Employees, EmployeeCourse assignment), ASP.NET Core Identity auth
- `Etms.Domain` — POCOs: `Course`, `Employee`, `EmployeeCourse` (mirrors the legacy `App_Code` classes)
- `Etms.Data` — EF Core `DbContext`, Identity store, migrations
- `Etms.Tests` — xUnit unit + integration tests (EF Core InMemory provider)

## Run locally with Docker Compose

```bash
cp .env.example .env
# edit .env and set a real ETMS_DB_PASSWORD (12+ chars)
docker compose up --build
```

The app applies pending EF Core migrations automatically on startup. Visit
`http://localhost:8080`, register an account (self-registration is enabled
for now — see Open items below), then sign in.

## Run locally without Docker

Requires a reachable SQL Server instance and the connection string set via
`ConnectionStrings:Etms` (e.g. through `dotnet user-secrets` or an
environment variable `ConnectionStrings__Etms`).

```bash
cd Etms.Web
dotnet run
```

## Tests

```bash
dotnet test Etms.Tests/Etms.Tests.csproj
```

## Data migration from the legacy app

`DataMigration/migrate-legacy-data.sql` copies existing rows from the
legacy `Frog` database into the new schema. Run once during cutover
(KAN-11), after migrations have created the new schema. See
`DataMigration/cutover-runbook.md` for the full parallel-run, cutover, and
rollback plan (KAN-11 / KAN-10 acceptance criterion 5).

## Open items (tracked in Jira, not silently decided here)

- Role enforcement (2026-09-19 human decision, implemented): Course/Employee
  add/edit/delete require the "Admin" role; listing pages stay open to any
  authenticated user.
- Self-registration (2026-09-19 human decision, implemented): the Identity
  Register page is overridden (`Areas/Identity/Pages/Account/Register.cshtml`)
  to require an existing Admin — the RCL's default page is `[AllowAnonymous]`
  and can't be locked down with an authorization convention alone, since
  `AllowAnonymous` always wins. A bootstrap admin account is seeded from
  `Bootstrap:AdminEmail`/`Bootstrap:AdminPassword` config to escape the
  resulting chicken-and-egg problem on first run — rotate that password
  immediately after first login.
- TLS (2026-09-19 human decision): a reverse proxy/load balancer terminates
  TLS in front of this container; the app trusts `X-Forwarded-*` headers
  (`ForwardedHeadersOptions.KnownIPNetworks`/`KnownProxies` are cleared since
  the proxy's address isn't known at build time — restrict them to the
  actual proxy's network per environment for defense-in-depth).
- Encryption at rest (2026-09-19, confirmed by human Engineering Lead):
  already handled by infrastructure outside this app/repo.
- CI pipeline wiring is out of scope for this agent (governance surface —
  `.github/workflows/**` and `bitbucket-pipelines.yml` are off-limits); a
  human/platform owner needs to add a workflow that runs `dotnet build`,
  `dotnet test`, and the container image build.
- Container image build is written but not yet verified against a running
  Docker daemon in this environment — verify with `docker compose up --build`
  before relying on it.
