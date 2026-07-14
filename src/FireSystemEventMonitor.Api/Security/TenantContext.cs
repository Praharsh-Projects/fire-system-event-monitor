namespace FireSystemEventMonitor.Api.Security;

public interface ITenantContext
{
    string TenantId { get; set; }
    bool IsResolved { get; }
}

public sealed class TenantContext : ITenantContext
{
    private string _tenantId = string.Empty;

    public string TenantId
    {
        get => _tenantId;
        set => _tenantId = string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Tenant ID cannot be empty.", nameof(value))
            : value;
    }

    public bool IsResolved => !string.IsNullOrWhiteSpace(_tenantId);
}

