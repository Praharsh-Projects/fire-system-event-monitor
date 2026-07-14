using FireSystemEventMonitor.Api.Domain;
using FireSystemEventMonitor.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace FireSystemEventMonitor.Api.Infrastructure;

public sealed class FireMonitorDbContext(
    DbContextOptions<FireMonitorDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<FireEvent> FireEvents => Set<FireEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Incident>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DeviceId).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Severity).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.Status, x.UpdatedAt });
            entity.HasQueryFilter(x => x.TenantId == tenantContext.TenantId);
        });

        modelBuilder.Entity<FireEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DeviceId).HasMaxLength(80).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.DeviceId, x.OccurredAt });
            entity.HasQueryFilter(x => x.TenantId == tenantContext.TenantId);
            entity.HasOne(x => x.Incident)
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.IncidentId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (!tenantContext.IsResolved)
        {
            throw new InvalidOperationException("Tenant context must be resolved before persistence.");
        }

        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = tenantContext.TenantId;
            }
            else if (entry.State == EntityState.Modified && entry.Entity.TenantId != tenantContext.TenantId)
            {
                throw new InvalidOperationException("Cross-tenant writes are not permitted.");
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

