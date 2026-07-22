# Power Platform integration design

## Purpose

The integration exposes the existing incident API as three Power Platform actions so a maker can build a Power Apps
review screen or a Power Automate triage flow without copying API credentials into formulas or workflow steps.

```text
Power Apps canvas app / Power Automate cloud flow
                    |
                    | custom connector actions
                    | dynamic HTTPS host + tenant/API-key connection values
                    v
          ASP.NET Core incident API
                    |
                    v
          SQLite local / SQL Server profile
```

## Connector actions

| Operation ID | HTTP operation | Maker use |
| --- | --- | --- |
| `GetIncidents` | `GET /api/incidents` | Populate an incident gallery or automation filter. |
| `RecordEvent` | `POST /api/events` | Submit a validated alarm, fault, warning, or restored event. |
| `AcknowledgeIncident` | `POST /api/incidents/{id}/acknowledge` | Record an operator acknowledgement. |

The connection collects three environment-specific values:

- `api_host`: deployed HTTPS API host, without scheme or path;
- `tenant_id`: workspace value injected into `X-Tenant-Id`; and
- `api_key`: secure value emitted as `X-Api-Key` by the connector authentication definition.

The repository deliberately uses a dynamic-host policy so source does not hard-code a deployment URL. Production
delivery should replace header API keys with reviewed Microsoft Entra authentication and authorization.

## Power Apps design

The maker design in [`power-platform/power-apps`](../power-platform/power-apps) covers gallery loading,
acknowledgement, refresh, and visible error handling. Connection values belong in the connection reference, not in
Power Fx formulas. The app should be created inside a solution and tested under the target environment's data loss
prevention policy.

## Power Automate design

The structured blueprint in [`power-platform/power-automate`](../power-platform/power-automate) describes a
recurrence-triggered triage flow:

1. call `GetIncidents`;
2. filter open critical incidents;
3. request an operator decision through an organization-approved connector; and
4. call `AcknowledgeIncident` only after an explicit acknowledgement decision.

The blueprint is intentionally marked `importable: false`. A real cloud flow must be created inside a solution so
Power Platform can produce environment-specific connection references and exported solution metadata.

## Verification and release gates

Repository verification checks JSON syntax, Microsoft connector schemas, operation IDs, secure connection parameter
types, header policies, flow references, and the explicit non-importable boundary. Before release, a development
tenant is still required to:

1. import the connector into a solution;
2. create a connection to a deployed HTTPS API;
3. exercise all actions and error responses;
4. build and test the canvas app and cloud flow;
5. review DLP, sharing, authorization, secret rotation, and support ownership; and
6. export a managed solution for downstream environments.

No live tenant, cloud flow, canvas-app deployment, Azure resource, client data, or production outcome is claimed.
