using System.Text.Json;

namespace FireSystemEventMonitor.Api.Tests;

public sealed class PowerPlatformConnectorContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void ConnectorDefinitionExposesTheSupportedApiActions()
    {
        using var document = LoadJson("power-platform", "connector", "apiDefinition.swagger.json");
        var root = document.RootElement;

        Assert.Equal("2.0", root.GetProperty("swagger").GetString());
        var securityDefinition = root.GetProperty("securityDefinitions").GetProperty("api_key");
        Assert.Equal("apiKey", securityDefinition.GetProperty("type").GetString());
        Assert.Equal("header", securityDefinition.GetProperty("in").GetString());
        Assert.Equal("X-Api-Key", securityDefinition.GetProperty("name").GetString());

        var expectedOperations = new Dictionary<string, (string Method, string OperationId)>
        {
            ["/api/incidents"] = ("get", "GetIncidents"),
            ["/api/events"] = ("post", "RecordEvent"),
            ["/api/incidents/{id}/acknowledge"] = ("post", "AcknowledgeIncident")
        };

        var operationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (path, expected) in expectedOperations)
        {
            var operation = root.GetProperty("paths").GetProperty(path).GetProperty(expected.Method);
            var operationId = operation.GetProperty("operationId").GetString();
            Assert.Equal(expected.OperationId, operationId);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.True(operationIds.Add(operationId!));
        }
    }

    [Fact]
    public void ConnectorPropertiesInjectHostTenantAndSecretWithoutHardCodedCredentials()
    {
        using var document = LoadJson("power-platform", "connector", "apiProperties.json");
        var properties = document.RootElement.GetProperty("properties");
        var connectionParameters = properties.GetProperty("connectionParameters");

        Assert.Equal("string", connectionParameters.GetProperty("api_host").GetProperty("type").GetString());
        Assert.Equal("string", connectionParameters.GetProperty("tenant_id").GetProperty("type").GetString());
        Assert.Equal("securestring", connectionParameters.GetProperty("api_key").GetProperty("type").GetString());

        var policies = properties.GetProperty("policyTemplateInstances").EnumerateArray().ToList();
        var hostPolicy = Assert.Single(policies, policy =>
            policy.GetProperty("templateId").GetString() == "dynamichosturl");
        Assert.Contains("@connectionParameters('api_host')", hostPolicy.GetRawText(), StringComparison.Ordinal);

        var tenantPolicy = Assert.Single(policies, policy =>
            policy.GetProperty("templateId").GetString() == "setheader");
        Assert.Contains("X-Tenant-Id", tenantPolicy.GetRawText(), StringComparison.Ordinal);
        Assert.Contains("@connectionParameters('tenant_id')", tenantPolicy.GetRawText(), StringComparison.Ordinal);

        var serialized = document.RootElement.GetRawText();
        Assert.DoesNotContain("demo-key", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alpha-key", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FlowBlueprintReferencesOnlyPublishedConnectorOperationsAndStatesItsDeploymentBoundary()
    {
        using var connector = LoadJson("power-platform", "connector", "apiDefinition.swagger.json");
        var publishedOperations = connector.RootElement.GetProperty("paths")
            .EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject())
            .Where(operation => operation.Name is "get" or "post" or "put" or "patch" or "delete")
            .Select(operation => operation.Value.GetProperty("operationId").GetString())
            .Where(operationId => operationId is not null)
            .ToHashSet(StringComparer.Ordinal);

        using var blueprint = LoadJson(
            "power-platform",
            "power-automate",
            "critical-incident-triage.flow.json");
        var connectorSteps = blueprint.RootElement.GetProperty("steps")
            .EnumerateArray()
            .Where(step => step.TryGetProperty("connectorOperationId", out _))
            .ToList();

        Assert.NotEmpty(connectorSteps);
        Assert.All(connectorSteps, step =>
            Assert.Contains(step.GetProperty("connectorOperationId").GetString(), publishedOperations));
        Assert.False(blueprint.RootElement.GetProperty("importable").GetBoolean());
        Assert.NotEmpty(blueprint.RootElement.GetProperty("deploymentGates").EnumerateArray());
    }

    private static JsonDocument LoadJson(params string[] pathSegments)
    {
        var path = pathSegments.Aggregate(RepositoryRoot, Path.Combine);
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var connectorPath = Path.Combine(
                directory.FullName,
                "power-platform",
                "connector",
                "apiDefinition.swagger.json");
            if (File.Exists(connectorPath))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test output path.");
    }
}
