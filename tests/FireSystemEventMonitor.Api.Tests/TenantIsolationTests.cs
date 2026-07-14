using System.Net;
using System.Net.Http.Json;
using FireSystemEventMonitor.Api.Domain;

namespace FireSystemEventMonitor.Api.Tests;

public sealed class TenantIsolationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task RequestsWithoutTenantCredentialsAreRejected()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/incidents");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EventsWithInvalidPayloadAreRejected()
    {
        using var client = CreateTenantClient("alpha", "alpha-key");
        var response = await client.PostAsJsonAsync("/api/events", new
        {
            deviceId = "x",
            eventType = "Offline",
            message = "x"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task IncidentsAreIsolatedByTenantAndCanBeAcknowledged()
    {
        using var alpha = CreateTenantClient("alpha", "alpha-key");
        using var beta = CreateTenantClient("beta", "beta-key");

        var createResponse = await alpha.PostAsJsonAsync("/api/events", new CreateFireEventRequest(
            "panel-a-17", "Alarm", "Smoke detector activated", DateTimeOffset.Parse("2026-07-14T08:00:00Z")));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var alphaIncidents = await alpha.GetFromJsonAsync<List<IncidentResponse>>("/api/incidents");
        var betaIncidents = await beta.GetFromJsonAsync<List<IncidentResponse>>("/api/incidents");

        var incident = Assert.Single(alphaIncidents!);
        Assert.Empty(betaIncidents!);
        Assert.Equal(IncidentSeverities.Critical, incident.Severity);

        var acknowledgeResponse = await alpha.PostAsync($"/api/incidents/{incident.Id}/acknowledge", null);
        acknowledgeResponse.EnsureSuccessStatusCode();
        var acknowledged = await acknowledgeResponse.Content.ReadFromJsonAsync<IncidentResponse>();
        Assert.Equal(IncidentStatuses.Acknowledged, acknowledged!.Status);
    }

    [Fact]
    public async Task RestoredEventResolvesTheOpenIncident()
    {
        using var client = CreateTenantClient("alpha", "alpha-key");
        await client.PostAsJsonAsync("/api/events", new CreateFireEventRequest(
            "panel-a-18", "Fault", "Loop communication fault", null));
        await client.PostAsJsonAsync("/api/events", new CreateFireEventRequest(
            "panel-a-18", "Restored", "Loop communication restored", null));

        var incidents = await client.GetFromJsonAsync<List<IncidentResponse>>("/api/incidents");
        var incident = Assert.Single(incidents!, x => x.DeviceId == "panel-a-18");
        Assert.Equal(IncidentStatuses.Resolved, incident.Status);
        Assert.Equal(2, incident.EventCount);
    }

    private HttpClient CreateTenantClient(string tenantId, string apiKey)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return client;
    }
}
