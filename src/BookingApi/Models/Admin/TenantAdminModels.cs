using BookingApi.Domain.Entities;

namespace BookingApi.Models.Admin;

public record TenantResponse(Guid Id, string Name, bool HasApiKey);

public record CreateTenantRequest(string Name);

public record RenameTenantRequest(string Name);

public record TenantApiKeyResponse(string ApiKey);

public record TenantMemberResponse(string UserId, string UserName, string Email, string Role);

public record TenantInviteResponse(
    string UserId,
    string UserName,
    string Email,
    string Role,
    string? TemporaryPassword
);

public record TenantMemberPasswordResponse(string TemporaryPassword);

public record AddTenantMemberRequest(string Email, string Role);

public record SetTenantMemberRoleRequest(string Role);

public static class TenantAdminExtensions
{
    public static TenantResponse ToResponse(this Tenant tenant) =>
        new(tenant.Id, tenant.Name, tenant.HasApiKey);

    public static TenantMemberResponse ToResponse(this TenantMember member) =>
        new(member.UserId, member.UserName, member.Email, member.Role);

    public static TenantInviteResponse ToResponse(this TenantInvite invite) =>
        new(
            invite.Member.UserId,
            invite.Member.UserName,
            invite.Member.Email,
            invite.Member.Role,
            invite.TemporaryPassword
        );
}
