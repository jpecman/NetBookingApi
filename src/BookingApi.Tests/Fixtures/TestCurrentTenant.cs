using BookingApi.Infrastructure.DbEntities;
using BookingApi.Infrastructure.Tenancy;

namespace BookingApi.Tests.Fixtures;

/// <summary>
/// Mutable tenant context: tests switch the acting tenant by setting
/// <see cref="TenantId"/>; queries pick the change up immediately.
/// </summary>
public class TestCurrentTenant : ICurrentTenant
{
    public Guid TenantId { get; set; } = TenantDb.DefaultTenantId;
}
