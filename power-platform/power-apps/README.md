# Power Apps maker design

The custom connector can support a small canvas-app incident review screen after the API and connector are deployed
to a development environment.

## Screen behavior

1. Add the `Fire System Event Monitor` custom connector as a data source.
2. Load tenant incidents when the screen becomes visible.
3. Bind a gallery to the local incident collection and show severity, status, device, summary, and update time.
4. Show the acknowledge action only for an open incident.
5. Refresh the collection after a successful acknowledgement and surface connector errors to the operator.

Representative Power Fx expressions, to be adjusted to the generated connector instance name:

```powerfx
// Screen.OnVisible
IfError(
    ClearCollect(colIncidents, FireSystemEventMonitor.GetIncidents()),
    Notify("Incidents could not be loaded.", NotificationType.Error)
)

// Acknowledge button.DisplayMode
If(ThisItem.status = "Open", DisplayMode.Edit, DisplayMode.Disabled)

// Acknowledge button.OnSelect
IfError(
    FireSystemEventMonitor.AcknowledgeIncident(ThisItem.id);
    ClearCollect(colIncidents, FireSystemEventMonitor.GetIncidents()),
    Notify("The incident could not be acknowledged.", NotificationType.Error)
)
```

## Delivery gates

- Build the app inside a solution and use connection references for environment-specific values.
- Never embed the tenant API key in formulas, controls, or source files.
- Apply the organization's data loss prevention policy before enabling other connectors.
- Test empty, unauthorized, validation-error, and service-unavailable states in a non-production environment.

This repository provides the connector contract and maker design. It does not contain an exported `.msapp` file and
does not claim that a Power Apps application was imported or deployed to a tenant.
