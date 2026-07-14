# Fire System Event Monitor

A full-stack engineering workbench for ingesting fire-device events and managing incident state in isolated tenant workspaces. The project focuses on the concerns that make a multi-tenant SaaS application defensible: authenticated tenant resolution, query-level data isolation, input validation, explicit incident transitions, automated tests, and repeatable delivery checks.

## System behavior

- Accepts `Alarm`, `Fault`, `Warning`, and `Restored` events from identified fire-system devices.
- Opens one active incident per device, escalates severity when a higher-priority event arrives, and resolves the incident on a `Restored` event.
- Lets an operator acknowledge an open incident through the React interface.
- Requires `X-Tenant-Id` and `X-Api-Key` headers for application endpoints.
- Applies EF Core global query filters and write guards so one tenant cannot read or mutate another tenant's records.
- Uses SQLite for a zero-dependency local run and includes a compiled SQL Server provider and production configuration.

## Architecture

```text
React + TypeScript UI
        |
        | tenant headers + JSON
        v
ASP.NET Core minimal API
        |
        +-- TenantResolutionMiddleware (credential validation)
        +-- IncidentService (event and state-transition rules)
        +-- FireMonitorDbContext (tenant filters and write guard)
        |
        v
SQLite locally / SQL Server in production configuration
```

The API keeps HTTP, incident rules, security context, and persistence in separate modules. `TimeProvider` is injected so time-sensitive behavior can be made deterministic in tests. Terraform defines a Linux App Service and private-network SQL Server baseline with managed identity and Azure AD-only database administration.

## Technology choices

- **.NET 8 / C# / ASP.NET Core:** strongly typed backend and middleware pipeline.
- **Entity Framework Core:** shared domain model with SQLite and SQL Server providers.
- **React / TypeScript / Vite:** small single-page application with strict TypeScript checks.
- **xUnit + WebApplicationFactory:** unit and HTTP integration tests, including tenant-isolation checks.
- **Vitest + Testing Library:** component behavior tests.
- **Playwright:** browser test of the real UI and API workflow.
- **GitHub Actions:** build, unit, integration, frontend, and browser checks on pushes and pull requests.
- **Terraform:** validated Azure App Service and SQL Server infrastructure definition.

## Prerequisites

- .NET SDK 8
- Node.js 22 or newer
- npm

## Run locally

In one terminal:

```bash
dotnet run --project src/FireSystemEventMonitor.Api/FireSystemEventMonitor.Api.csproj --urls http://127.0.0.1:5080
```

In another terminal:

```bash
cd frontend
npm ci
npm run dev
```

Open `http://127.0.0.1:5173` and use the local-only credentials:

```text
Tenant ID: demo
API key: demo-key
```

The credentials in `appsettings.json` are intentionally limited to local evaluation. Production tenant credentials must be injected from a secret store or environment configuration.

## API example

```bash
curl -X POST http://127.0.0.1:5080/api/events \
  -H 'Content-Type: application/json' \
  -H 'X-Tenant-Id: demo' \
  -H 'X-Api-Key: demo-key' \
  -d '{"deviceId":"panel-a-17","eventType":"Alarm","message":"Smoke detector activated"}'
```

List the resulting incidents:

```bash
curl http://127.0.0.1:5080/api/incidents \
  -H 'X-Tenant-Id: demo' \
  -H 'X-Api-Key: demo-key'
```

Example response:

```json
[
  {
    "id": "generated-guid",
    "deviceId": "panel-a-17",
    "status": "Open",
    "severity": "Critical",
    "summary": "Smoke detector activated",
    "openedAt": "2026-07-14T08:00:00+00:00",
    "updatedAt": "2026-07-14T08:00:00+00:00",
    "eventCount": 1
  }
]
```

## Verification

Backend unit and integration tests:

```bash
dotnet test tests/FireSystemEventMonitor.Api.Tests/FireSystemEventMonitor.Api.Tests.csproj --configuration Release
```

Frontend component tests and production build:

```bash
cd frontend
npm ci
npm test
npm run build
```

Browser workflow test:

```bash
cd frontend
npx playwright install chromium
npm run test:e2e
```

Terraform validation:

```bash
terraform -chdir=infra init -backend=false
terraform -chdir=infra fmt -check
terraform -chdir=infra validate
```

The current verified suite contains 8 backend tests, 2 React component tests, and 1 browser workflow test.

## Security notes

- API keys are compared through fixed-time SHA-256 digests to avoid simple timing leakage.
- Tenant identity comes from validated credentials, not from request bodies.
- EF Core query filters apply tenant scope to incident and event reads.
- `SaveChangesAsync` assigns tenant ownership on inserts and rejects cross-tenant updates.
- Error responses omit exception detail outside development.
- Terraform disables public SQL Server network access and uses TLS 1.2 minimum plus Azure AD-only administration.

## Limitations and trade-offs

- Header API keys keep this repository self-contained; a production service should use an identity provider, short-lived tokens, secret rotation, and audited authorization policies.
- `EnsureCreated` simplifies local evaluation. Production delivery should use reviewed EF Core migrations.
- The Terraform definition validates locally but was not applied to an Azure subscription during this build.
- The UI is an operations workbench, not a replacement for certified fire alarm control equipment.
- Event delivery is synchronous. A larger system would place ingestion behind a durable broker and add idempotency keys.

