using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;

using LanguageExt;

namespace BookingApi.Domain.Gateways;

public interface ITenantGateway
{
    Task<Either<Error, Tenant[]>> GetAllAsync();

    Task<Either<Error, Tenant>> GetAsync(Guid id);

    Task<Either<Error, Tenant>> CreateAsync(string name);

    Task<Either<Error, Tenant>> RenameAsync(Guid id, string name);

    /// <summary>Generates a new API key for the tenant and returns it — the only
    /// time the key is available in plaintext.</summary>
    Task<Either<Error, string>> ResetApiKeyAsync(Guid id);

    Task<Either<Error, TenantMember[]>> GetMembersAsync(Guid tenantId);

    /// <summary>Invite by email: attaches a membership to the existing user with
    /// that email, or creates the user with a generated temporary password that
    /// must be changed at first login.</summary>
    Task<Either<Error, TenantInvite>> AddMemberAsync(Guid tenantId, string email, string role);

    /// <summary>Replaces the member's password with a generated temporary one
    /// (returned in plaintext exactly once) and forces a change at next login.</summary>
    Task<Either<Error, string>> ResetMemberPasswordAsync(Guid tenantId, string userId);

    Task<Either<Error, TenantMember>> SetMemberRoleAsync(Guid tenantId, string userId, string role);

    Task<Either<Error, bool>> RemoveMemberAsync(Guid tenantId, string userId);
}
