using FireSystemEventMonitor.Api.Domain;
using FireSystemEventMonitor.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FireSystemEventMonitor.Api.Services;

public interface IIncidentService
{
    Task<IReadOnlyList<IncidentResponse>> ListAsync(CancellationToken cancellationToken);
    Task<EventResult> RecordEventAsync(CreateFireEventRequest request, CancellationToken cancellationToken);
    Task<IncidentResponse?> AcknowledgeAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class IncidentService(FireMonitorDbContext dbContext, TimeProvider timeProvider) : IIncidentService
{
    private static readonly IReadOnlyDictionary<string, int> SeverityRanks =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [IncidentSeverities.Low] = 1,
            [IncidentSeverities.Medium] = 2,
            [IncidentSeverities.High] = 3,
            [IncidentSeverities.Critical] = 4
        };

    public async Task<IReadOnlyList<IncidentResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var incidents = await dbContext.Incidents
            .AsNoTracking()
            .Select(x => ToResponse(x, x.Events.Count))
            .ToListAsync(cancellationToken);

        return incidents.OrderByDescending(x => x.UpdatedAt).ToList();
    }

    public async Task<EventResult> RecordEventAsync(
        CreateFireEventRequest request,
        CancellationToken cancellationToken)
    {
        var occurredAt = request.OccurredAt ?? timeProvider.GetUtcNow();
        var normalizedType = NormalizeEventType(request.EventType);
        var incident = await dbContext.Incidents
            .Include(x => x.Events)
            .SingleOrDefaultAsync(
                x => x.DeviceId == request.DeviceId && x.Status != IncidentStatuses.Resolved,
                cancellationToken);

        if (normalizedType == "Restored")
        {
            if (incident is not null)
            {
                incident.Status = IncidentStatuses.Resolved;
                incident.Summary = request.Message;
                incident.UpdatedAt = occurredAt;
            }
        }
        else if (incident is null)
        {
            incident = new Incident
            {
                DeviceId = request.DeviceId,
                Status = IncidentStatuses.Open,
                Severity = SeverityFor(normalizedType),
                Summary = request.Message,
                OpenedAt = occurredAt,
                UpdatedAt = occurredAt
            };
            dbContext.Incidents.Add(incident);
        }
        else
        {
            var incomingSeverity = SeverityFor(normalizedType);
            if (SeverityRanks[incomingSeverity] > SeverityRanks[incident.Severity])
            {
                incident.Severity = incomingSeverity;
            }

            incident.Summary = request.Message;
            incident.UpdatedAt = occurredAt;
        }

        var fireEvent = new FireEvent
        {
            DeviceId = request.DeviceId,
            EventType = normalizedType,
            Message = request.Message,
            OccurredAt = occurredAt,
            Incident = incident
        };
        dbContext.FireEvents.Add(fireEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new EventResult(
            fireEvent.Id,
            incident is null ? null : ToResponse(incident, incident.Events.Count));
    }

    public async Task<IncidentResponse?> AcknowledgeAsync(Guid id, CancellationToken cancellationToken)
    {
        var incident = await dbContext.Incidents
            .Include(x => x.Events)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (incident is null)
        {
            return null;
        }

        if (incident.Status == IncidentStatuses.Open)
        {
            incident.Status = IncidentStatuses.Acknowledged;
            incident.UpdatedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToResponse(incident, incident.Events.Count);
    }

    public static string SeverityFor(string eventType) => NormalizeEventType(eventType) switch
    {
        "Alarm" => IncidentSeverities.Critical,
        "Fault" => IncidentSeverities.High,
        "Warning" => IncidentSeverities.Medium,
        "Restored" => IncidentSeverities.Low,
        _ => throw new ArgumentOutOfRangeException(nameof(eventType))
    };

    private static string NormalizeEventType(string eventType)
    {
        var match = new[] { "Alarm", "Fault", "Warning", "Restored" }
            .SingleOrDefault(x => string.Equals(x, eventType, StringComparison.OrdinalIgnoreCase));

        return match ?? throw new ArgumentException("Unsupported event type.", nameof(eventType));
    }

    private static IncidentResponse ToResponse(Incident incident, int eventCount) => new(
        incident.Id,
        incident.DeviceId,
        incident.Status,
        incident.Severity,
        incident.Summary,
        incident.OpenedAt,
        incident.UpdatedAt,
        eventCount);
}
