# Power Platform custom connector

This folder contains a source-controlled custom connector contract for Microsoft Power Apps and Power Automate. It
maps three existing API operations: list incidents, record an event, and acknowledge an incident.

The connector asks for an HTTPS API host, tenant ID, and tenant API key when a connection is created. Power Platform
policies route requests to the configured host and add `X-Tenant-Id`; the Swagger API-key definition adds
`X-Api-Key`.

## Validate

From the repository root:

```bash
python -m pip install -r requirements-dev.txt
python scripts/validate_power_platform.py
dotnet test tests/FireSystemEventMonitor.Api.Tests/FireSystemEventMonitor.Api.Tests.csproj --configuration Release
```

The Python validator checks both connector files against Microsoft Power Platform Connectors schemas pinned to an
exact upstream commit. The .NET tests additionally check operation IDs, authentication, connection policies, and the
Power Automate blueprint references.

## Import gate

After deploying the API to an HTTPS endpoint, authenticate the Power Platform CLI to a development environment and
create the connector:

```bash
pac auth create
pac connector create \
  --api-definition-file power-platform/connector/apiDefinition.swagger.json \
  --api-properties-file power-platform/connector/apiProperties.json \
  --solution-unique-name FireSystemOperations
```

Create a connection with the deployed host, tenant ID, and tenant secret, then test all three actions in the maker
portal before moving the solution to another environment.

No Power Platform tenant import, connection, or production API deployment is claimed by this repository.
