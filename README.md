# Fire System Event Monitor

A full-stack engineering workbench for ingesting fire-device events and managing incident state in isolated tenant workspaces. The project focuses on the concerns that make a multi-tenant SaaS application defensible: authenticated tenant resolution, query-level data isolation, input validation, explicit incident transitions, automated tests, and repeatable delivery checks.

## System behavior

- Accepts `Alarm`, `Fault`, `Warning`, and `Restored` events from identified fire-system devices.
- Opens one active incident per device, escalates severity when a higher-priority event arrives, and resolves the incident on a `Restored` event.
- Lets an operator acknowledge an open incident through the React interface.
- Requires `X-Tenant-Id` and `X-Api-Key` headers for application endpoints.
- Applies EF Core global query filters and write guards so one tenant cannot read or mutate another tenant's records.
- Uses SQLite for a zero-dependency local run and includes a compiled SQL Server provider and production configuration.
- Provides a version-controlled Power Platform custom connector contract for Power Apps and Power Automate actions.
- Provides a signed Dynamics 365 CE/Dataverse plug-in and a model-driven app JavaScript web resource for high-priority Case follow-up.

## Architecture

```text
React + TypeScript UI       Power Apps / Power Automate
        |                       | versioned custom connector
        +-----------+-----------+
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

Dynamics 365 CE Case form
        |
        +-- JavaScript web resource (formContext validation and guidance)
        +-- .NET Framework 4.6.2 IPlugin (pre-operation follow-up rule)
```

The API keeps HTTP, incident rules, security context, and persistence in separate modules. `TimeProvider` is injected so time-sensitive behavior can be made deterministic in tests. Terraform defines a Linux App Service and private-network SQL Server baseline with managed identity and Azure AD-only database administration.

## Technology choices

- **.NET 8 / C# / ASP.NET Core:** strongly typed backend and middleware pipeline.
- **Entity Framework Core:** shared domain model with SQLite and SQL Server providers.
- **React / TypeScript / Vite:** small single-page application with strict TypeScript checks.
- **xUnit + WebApplicationFactory:** unit and HTTP integration tests, including tenant-isolation checks.
- **Vitest + Testing Library:** component behavior tests.
- **Playwright:** browser test of the real UI and API workflow.
- **Power Platform:** custom connector metadata, Power Apps maker design, and a Power Automate triage-flow blueprint.
- **Dynamics 365 CE / Dataverse:** signed C# `IPlugin`, standard Case-table registration plan, JavaScript form events, and model-driven app release gates.
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
npm audit --audit-level=high
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

Power Platform contract validation:

```bash
python -m pip install -r requirements-dev.txt
python scripts/validate_power_platform.py
```

Dynamics 365 CE extension checks:

```bash
dotnet build dynamics365/plugins/FireSystemEventMonitor.Dataverse.Plugin/FireSystemEventMonitor.Dataverse.Plugin.csproj --configuration Release
npm ci --prefix dynamics365/webresources
npm run check --prefix dynamics365/webresources
npm test --prefix dynamics365/webresources
```

The current verified suite contains 12 backend tests, 2 React component tests, 1 browser workflow test, 9 Dataverse
plug-in tests, and 7 model-driven app JavaScript tests. The Power Platform validator checks the custom connector
against Microsoft schemas pinned to a specific upstream commit, checks the structured Power Automate blueprint
against its local schema, and validates the Dynamics 365 CE deployment manifest and referenced source files.

## Power Platform integration

The [`power-platform`](power-platform) folder contains:

- a Swagger 2.0 custom connector for listing incidents, recording events, and acknowledging incidents;
- connection policies for a dynamic HTTPS host and tenant header, with the API key held as a secure connection value;
- a Power Apps screen design with Power Fx examples; and
- a non-importable Power Automate flow blueprint with explicit environment, connection-reference, DLP, and test gates.

See the [Power Platform integration guide](docs/power-platform-integration.md). The repository validates source
contracts only; it does not claim a tenant import, active cloud flow, deployed canvas app, or Azure deployment.

## Dynamics 365 CE extension

The [`dynamics365`](dynamics365) folder contains:

- a signed .NET Framework 4.6.2 plug-in that applies a deterministic follow-up rule to the standard Case table;
- unit tests for create, update, pre-image, existing-deadline, priority, recursion, and failure paths;
- a JavaScript web resource using `executionContext.getFormContext()` for Case form events;
- Node tests covering high-priority, normal-priority, missing-value, missing-control, and event-handler behavior; and
- a schema-validated deployment manifest recording plug-in steps, filtering columns, pre-image, web resource, and form handlers.

The extension source and CI are reproducible, but no Dataverse tenant import, plug-in registration, model-driven app
publication, customer deployment, or live Dynamics 365 CE execution is claimed.

## Requirements and verification traceability

The [requirements and verification traceability matrix](docs/requirements-traceability.md) links the system behavior described in this repository to implementation locations and automated test evidence. It is an engineering review artifact for this repository, not a medical-device QMS record, IEC 62304 evidence, regulatory submission, or clinical validation.

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
- Power Platform assets were schema- and contract-validated but not imported into a tenant or exercised with a live
  Power Apps or Power Automate connection.
- Dynamics 365 CE plug-in and JavaScript assets build and test in CI, but were not registered or executed in an
  authenticated Dataverse environment; the deployment manifest is not an exported solution package.
- The UI is an operations workbench, not a replacement for certified fire alarm control equipment.
- Event delivery is synchronous. A larger system would place ingestion behind a durable broker and add idempotency keys.
