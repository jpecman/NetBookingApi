using System.Security.Claims;

using BookingApi.Infrastructure.DbEntities;
using BookingApi.Infrastructure.Tenancy;

namespace BookingApi.Auth;

/// <summary>
/// Resolves the acting tenant from the principal's tenant claim (set by both
/// the JWT and API-key schemes). Falls back to the default tenant only outside
/// an authenticated request, e.g. startup seeding.
/// </summary>
public class ClaimsCurrentTenant(IHttpContextAccessor httpContextAccessor) : ICurrentTenant
{
    public Guid TenantId =>
        Guid.TryParse(
            httpContextAccessor.HttpContext?.User.FindFirstValue(TenantClaims.TenantId),
            out var tenantId
        )
            ? tenantId
            : TenantDb.DefaultTenantId;
}
