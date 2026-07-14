using FireSystemEventMonitor.Api.Domain;
using FireSystemEventMonitor.Api.Services;

namespace FireSystemEventMonitor.Api.Tests;

public sealed class IncidentServiceTests
{
    [Theory]
    [InlineData("Alarm", IncidentSeverities.Critical)]
    [InlineData("fault", IncidentSeverities.High)]
    [InlineData("WARNING", IncidentSeverities.Medium)]
    [InlineData("Restored", IncidentSeverities.Low)]
    public void SeverityFor_MapsSupportedEventTypes(string eventType, string expected)
    {
        Assert.Equal(expected, IncidentService.SeverityFor(eventType));
    }

    [Fact]
    public void SeverityFor_RejectsUnknownEventType()
    {
        Assert.Throws<ArgumentException>(() => IncidentService.SeverityFor("Offline"));
    }
}

