namespace BookingApi.Domain.Entities;

/// <summary>Result of inviting a member: TemporaryPassword is set only when the
/// user was created by the invite, and is never available again.</summary>
public record TenantInvite(TenantMember Member, string? TemporaryPassword);
