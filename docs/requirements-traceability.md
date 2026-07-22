# Requirements and Verification Traceability

## Scope and status

This matrix connects the intended behavior of the Fire System Event Monitor to implementation locations and repeatable verification. It is an engineering review artifact for this repository. It is not a medical-device QMS artifact, IEC 62304 evidence, a regulatory submission, a design-transfer record, or clinical validation.

The system is a multi-tenant operations workbench that accepts fire-system events, maintains incident state, and exposes those incidents through a React interface. The table records the behavior that is implemented and exercised by the repository's automated checks as of the linked commit.

## Traceability matrix

| ID | Intended behavior | Design and implementation evidence | Automated verification evidence | Status and limits |
| --- | --- | --- | --- | --- |
| FR-01 | Application endpoints require valid tenant credentials. | `Security/TenantResolutionMiddleware.cs` reads `X-Tenant-Id` and `X-Api-Key`, rejects invalid credentials, and resolves the tenant context before the request continues. | `TenantIsolationTests.RequestsWithoutTenantCredentialsAreRejected` sends an unauthenticated request and expects `401 Unauthorized`. | Verified for the local test configuration. This repository uses header API keys rather than a production identity provider. |
| FR-02 | A valid event request is structurally validated before incident processing. | `Domain/Contracts.cs` constrains device ID, event type, and message; `Program.cs` runs data-annotation validation before `IIncidentService.RecordEventAsync`. | `TenantIsolationTests.EventsWithInvalidPayloadAreRejected` submits short fields and an unsupported event type and expects `400 Bad Request`. | Verified for the supplied invalid case. It is not exhaustive fuzz or security testing. |
| FR-03 | Alarm, fault, warning, and restored events drive deterministic incident state. | `Services/IncidentService.cs` normalizes supported event types, maps severity, opens or updates an incident, and resolves it for `Restored`. `TimeProvider` is injected for deterministic time behavior. | `IncidentServiceTests.SeverityFor_MapsSupportedEventTypes`, `IncidentServiceTests.SeverityFor_RejectsUnknownEventType`, and `TenantIsolationTests.RestoredEventResolvesTheOpenIncident`. | Verified for severity mapping, unsupported type rejection, and a restoration workflow. |
| FR-04 | One tenant cannot read or mutate another tenant's incident data. | `Infrastructure/FireMonitorDbContext.cs` applies global tenant query filters, assigns tenant IDs on inserts, and rejects cross-tenant updates. | `TenantIsolationTests.IncidentsAreIsolatedByTenantAndCanBeAcknowledged` creates data for one tenant and verifies the second tenant receives an empty list. | Verified through the HTTP integration path; this is not a formal penetration test. |
| FR-05 | An operator can acknowledge an open incident. | `Services/IncidentService.cs` changes `Open` incidents to `Acknowledged`; `Program.cs` maps the acknowledgement endpoint; `frontend/src/App.tsx` exposes the operator action. | `TenantIsolationTests.IncidentsAreIsolatedByTenantAndCanBeAcknowledged` verifies the API state transition. `frontend/e2e/incident-workflow.spec.ts` verifies the UI workflow. | Verified through API integration and one browser workflow. |
| FR-06 | The web interface can connect to a tenant workspace, show incidents, and reject missing credentials before connecting. | `frontend/src/App.tsx` stores the tenant session, fetches incidents, renders incident state, and validates missing credentials. | `frontend/src/App.test.tsx` verifies incident rendering and missing-credential feedback. | Verified as component behavior with mocked fetch responses. |
| FR-07 | Power Apps and Power Automate makers can use a version-controlled connector contract for the incident API. | `power-platform/connector` defines three actions, secure API-key authentication, a dynamic host, and tenant-header policy. `power-platform/power-apps` and `power-platform/power-automate` document app and automation designs. | `scripts/validate_power_platform.py` validates artifacts against pinned Microsoft and local schemas. `PowerPlatformConnectorContractTests` checks action IDs, security, connection policies, secret boundaries, and flow references. | Source contracts are verified. Import, connection creation, live flow execution, and canvas-app deployment require a Power Platform development tenant and remain external gates. |

## Verification workflow

Run the checks from the repository root:

```bash
dotnet test tests/FireSystemEventMonitor.Api.Tests/FireSystemEventMonitor.Api.Tests.csproj --configuration Release

python -m pip install -r requirements-dev.txt
python scripts/validate_power_platform.py

cd frontend
npm ci
npm test
npx playwright install chromium
npm run test:e2e
npm run build
```

The current suite contains 12 backend test cases, 2 React component tests, and 1 browser workflow test. GitHub Actions runs the same Power Platform contract, backend, component, production-build, and browser checks for pushes to `main` and pull requests.

## Change-impact practice

When changing a behavior in the matrix, update the intended-behavior row, the implementation reference, and its automated evidence together. A changed requirement without a mapped verification result remains an open review item. This keeps the matrix useful for engineering discussion while avoiding a claim of formal regulated-product compliance.

## Known boundaries

- The application is an operations workbench and is not certified fire alarm control equipment.
- Infrastructure configuration is defined with Terraform and may be validated locally; it has not been applied to an Azure subscription for this repository.
- Power Platform connector and workflow source contracts have not been imported into or executed in a live tenant.
- The matrix records repository evidence only. It does not replace risk management, clinical evaluation, cybersecurity assessment, design controls, audits, or any medical-device quality process.
