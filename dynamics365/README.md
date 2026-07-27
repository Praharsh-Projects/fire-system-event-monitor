# Dynamics 365 CE extension package

This folder contains source-controlled Dynamics 365 Customer Engagement and Dataverse extensions for the fire-incident workflow. It adds server-side business logic through a C# `IPlugin` implementation and client-side form behavior through a JavaScript web resource.

The code follows the current Microsoft extension model:

- the plug-in builds as a signed .NET Framework 4.6.2 assembly against `Microsoft.CrmSdk.CoreAssemblies`;
- the plug-in is stateless, guards against recursive execution, uses a pre-image for partial updates, and writes trace messages;
- the model-driven app script obtains `formContext` from the execution context instead of using deprecated `Xrm.Page`;
- the deployment manifest records the solution, assembly, steps, filtering attributes, pre-image, web resource, and form handlers.

## Business behavior

The `EnsureHighPriorityFollowUpPlugin` runs synchronously in the Dataverse pre-operation stage for `Create` and `Update` messages on the standard Case (`incident`) table. When a case has the standard High priority option and no existing `followupby` value, it assigns a four-hour follow-up deadline. Existing deadlines are preserved.

The `fsm_/scripts/incident-form.js` web resource applies the same form rule:

- High-priority cases make `followupby` required.
- An empty deadline produces an inline form notification.
- Normal-priority cases return the column to optional.
- Missing form controls or execution context are handled without throwing a form error.

## Verification

Validate the deployment manifest and referenced source files:

```bash
python -m pip install -r requirements-dev.txt
python scripts/validate_power_platform.py
```

Run the JavaScript checks:

```bash
npm ci --prefix dynamics365/webresources
npm run check --prefix dynamics365/webresources
npm test --prefix dynamics365/webresources
```

Build the plug-in assembly on any platform with a .NET 8 SDK:

```bash
dotnet build \
  dynamics365/plugins/FireSystemEventMonitor.Dataverse.Plugin/FireSystemEventMonitor.Dataverse.Plugin.csproj \
  --configuration Release
```

The .NET Framework plug-in tests run in the Windows GitHub Actions job:

```powershell
dotnet test `
  dynamics365/plugins/FireSystemEventMonitor.Dataverse.Plugin.Tests/FireSystemEventMonitor.Dataverse.Plugin.Tests.csproj `
  --configuration Release
```

## Development signing key

`FireSystemEventMonitor.snk` is a repository-local development key used only to produce a strong-named evaluation assembly. It is not an organizational trust credential. A customer delivery must replace it with a protected signing key controlled by that customer's release process.

## Registration and release gates

The reviewed registration plan is in `deployment-manifest.json`.

1. Create or select an unmanaged `FireSystemOperations` solution in a development environment.
2. Build the signed plug-in assembly and register its type.
3. Register the `Create` and `Update` steps exactly as described by the manifest, including the `PreImage` on `Update`.
4. add the JavaScript file as `fsm_/scripts/incident-form.js`;
5. add the web resource to the Case main form and register the named `OnLoad` and `prioritycode` `OnChange` handlers with the execution context enabled;
6. exercise create, update, recursive-depth, missing-control, high-priority, normal-priority, and existing-deadline paths in a non-production environment;
7. export the complete solution and commit the official unpacked solution source before promoting it.

The repository has no authenticated Dataverse development environment. The manifest is a validated deployment contract, not an exported or importable solution. No tenant registration, model-driven app publication, customer deployment, or production use is claimed.
