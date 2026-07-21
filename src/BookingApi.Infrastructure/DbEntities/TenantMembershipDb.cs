using System.ComponentModel.DataAnnotations;

namespace BookingApi.Infrastructure.DbEntities;

/// <summary>
/// Assigns a user to a tenant with a role. Deliberately not tenant-filtered:
/// it is read during login, before any tenant context exists. A unique index
/// on UserId enforces the one-tenant-per-user policy; drop it (and add a
/// tenant picker at login) when multi-tenant users become real.
/// </summary>
public class TenantMembershipDb
{
    public string UserId { get; set; } = null!;

    public Guid TenantId { get; set; }

    public string Role { get; set; } = null!;

    [Timestamp]
    public uint Version { get; set; }
}
