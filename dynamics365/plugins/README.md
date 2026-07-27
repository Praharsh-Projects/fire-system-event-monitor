# Dataverse plug-in

`EnsureHighPriorityFollowUpPlugin` is a signed .NET Framework 4.6.2 Dataverse plug-in for the Dynamics 365 CE Case table.

Registration intent:

- Table: `incident`
- Messages: `Create` and `Update`
- Stage: Pre-operation
- Mode: Synchronous
- Update filtering columns: `prioritycode`, `followupby`
- Update pre-image alias: `PreImage`
- Update pre-image columns: `prioritycode`, `followupby`

The plug-in assigns a four-hour `followupby` value only when the resolved case priority is High and neither the target nor pre-image already contains a deadline. It is stateless, skips recursive depth, and produces trace messages without writing sensitive case data.

The exact source-controlled registration plan and tenant boundary are documented in `../deployment-manifest.json` and `../README.md`.
