namespace BookingApi.Infrastructure;

public class UserRoles
{
    /// <summary>
    /// Platform-level role (ASP.NET Identity, tenant-independent): may manage
    /// tenants and memberships. Not assignable to a membership.
    /// </summary>
    public const string SuperAdmin = "SuperAdmin";

    public const string Administrator = "Administrator";

    public const string Content = "Content";

    /// <summary>Roles a tenant membership may carry.</summary>
    public static readonly string[] MembershipRoles = [Administrator, Content];
}
