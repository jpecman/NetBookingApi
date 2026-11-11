using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using BookingApi.Infrastructure.Tenancy;

using Microsoft.EntityFrameworkCore;

namespace BookingApi.Infrastructure.Services.LoginService;

/// <summary>
/// Checks a validated JWT against the database on every request, so a token
/// stops working as soon as what it was issued for changes:
/// - the user is deleted,
/// - the security stamp changes (Identity rotates it on password change and
///   admin password reset; code that removes SuperAdmin must rotate it explicitly),
/// - the membership the token names (tenant + role) is removed or its role changes.
/// </summary>
public class TokenSessionValidator(BookingApiDbContext dbContext)
{
    public async Task<bool> IsCurrentAsync(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = principal.FindFirstValue(LoginClaims.SecurityStamp);

        if (
            string.IsNullOrEmpty(userId)
            || string.IsNullOrEmpty(stamp)
            || !Guid.TryParse(principal.FindFirstValue(TenantClaims.TenantId), out var tenantId)
        )
        {
            return false;
        }

        // SuperAdmin is a global Identity role, not part of the membership; the
        // other role claim is the membership role.
        var membershipRoles = principal
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Where(r => r != UserRoles.SuperAdmin)
            .ToArray();

        if (membershipRoles.Length != 1)
        {
            return false;
        }

        var role = membershipRoles[0];

        // Neither table is tenant-filtered, so this doesn't read ICurrentTenant.
        return await dbContext.TenantMemberships.AnyAsync(m =>
            m.UserId == userId
            && m.TenantId == tenantId
            && m.Role == role
            && dbContext.Users.Any(u => u.Id == userId && u.SecurityStamp == stamp)
        );
    }
}
