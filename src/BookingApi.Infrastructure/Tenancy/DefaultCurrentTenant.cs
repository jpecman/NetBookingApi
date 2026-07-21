using BookingApi.Infrastructure.DbEntities;

namespace BookingApi.Infrastructure.Tenancy;

/// <summary>
/// Pins the acting tenant to the default tenant. Used outside the request
/// pipeline (BookingApi.Utils, test bootstrap); requests resolve the tenant
/// from the JWT claim via ClaimsCurrentTenant.
/// </summary>
public class DefaultCurrentTenant : ICurrentTenant
{
    public Guid TenantId => TenantDb.DefaultTenantId;
}
