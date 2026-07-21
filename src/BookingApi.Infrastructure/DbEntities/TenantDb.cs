using System.ComponentModel.DataAnnotations;

using BookingApi.Domain.Entities;

namespace BookingApi.Infrastructure.DbEntities;

public class TenantDb
{
    /// <summary>
    /// The tenant all pre-tenancy data was backfilled to; also the tenant
    /// every request acts as until tenant-aware auth lands.
    /// </summary>
    public static readonly Guid DefaultTenantId = new("00000000-0000-0000-0000-000000000001");

    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>
    /// SHA-256 hash of the tenant's API key (see ApiKeyHasher); null = no key
    /// issued. One key per tenant — rotation replaces it.
    /// </summary>
    public string? ApiKeyHash { get; set; }

    [Timestamp]
    public uint Version { get; set; }
}

public static class TenantExtensions
{
    public static Tenant ToDomain(this TenantDb entity)
    {
        return new Tenant(entity.Id, entity.Name, entity.ApiKeyHash != null);
    }
}
