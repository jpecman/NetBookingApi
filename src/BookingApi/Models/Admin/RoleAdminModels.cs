using BookingApi.Domain.Entities;

namespace BookingApi.Models.Admin;

public record RoleResponse(string Id, string Name);

public record CreateRoleRequest(string Name);

public static class RoleAdminExtensions
{
    public static RoleResponse ToResponse(this Role role) =>
        new(role.Id, role.Name);
}
