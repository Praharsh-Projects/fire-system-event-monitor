namespace FireSystemEventMonitor.Api.Domain;

public interface ITenantOwned
{
    string TenantId { get; set; }
}

public sealed class Incident : ITenantOwned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string Status { get; set; } = IncidentStatuses.Open;
    public string Severity { get; set; } = IncidentSeverities.Medium;
    public string Summary { get; set; } = string.Empty;
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<FireEvent> Events { get; set; } = [];
}

public sealed class FireEvent : ITenantOwned
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public Guid? IncidentId { get; set; }
    public Incident? Incident { get; set; }
}

public static class IncidentStatuses
{
    public const string Open = "Open";
    public const string Acknowledged = "Acknowledged";
    public const string Resolved = "Resolved";
}

public static class IncidentSeverities
{
    public const string Critical = "Critical";
    public const string High = "High";
    public const string Medium = "Medium";
    public const string Low = "Low";
}

