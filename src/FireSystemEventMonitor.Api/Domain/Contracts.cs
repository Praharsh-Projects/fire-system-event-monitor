using System.ComponentModel.DataAnnotations;

namespace FireSystemEventMonitor.Api.Domain;

public sealed record CreateFireEventRequest(
    [property: Required, StringLength(80, MinimumLength = 2)] string DeviceId,
    [property: Required, RegularExpression("Alarm|Fault|Warning|Restored")] string EventType,
    [property: Required, StringLength(500, MinimumLength = 2)] string Message,
    DateTimeOffset? OccurredAt);

public sealed record IncidentResponse(
    Guid Id,
    string DeviceId,
    string Status,
    string Severity,
    string Summary,
    DateTimeOffset OpenedAt,
    DateTimeOffset UpdatedAt,
    int EventCount);

public sealed record EventResult(Guid EventId, IncidentResponse? Incident);

