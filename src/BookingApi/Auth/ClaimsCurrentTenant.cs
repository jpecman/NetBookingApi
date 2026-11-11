using System.Security.Claims;

using BookingApi.Infrastructure.Tenancy;

namespace BookingApi.Auth;

/// <summary>
/// Resolves the acting tenant from the principal's tenant claim (set by both
/// the JWT and API-key schemes). Code running outside a request (e.g. startup
/// seeding) must pin the tenant explicitly via <see cref="Use"/>. There is no
/// implicit fallback: without a pinned tenant or a valid claim, reading
/// <see cref="TenantId"/> throws, so tenant-scoped queries fail loudly.
/// </summary>
public class ClaimsCurrentTenant(IHttpContextAccessor httpContextAccessor) : ICurrentTenant
{
    private Guid? _explicitTenantId;

    /// <summary>
    /// Pins the tenant for this scope, for code that runs outside a request.
    /// </summary>
    public void Use(Guid tenantId) => _explicitTenantId = tenantId;

    public Guid TenantId =>
        _explicitTenantId
        ?? (
            Guid.TryParse(
                httpContextAccessor.HttpContext?.User.FindFirstValue(TenantClaims.TenantId),
                out var tenantId
            )
                ? tenantId
                : throw new InvalidOperationException("No tenant in the current context.")
        );
}
