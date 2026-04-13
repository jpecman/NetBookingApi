using BookingApi.Domain.Entities;

namespace BookingApi.Models.Admin;

public record UserResponse(string Id, string UserName, string Email, string[] Roles);

public record CreateUserRequest(string UserName, string Email, string Password);

public static class UserAdminExtensions
{
    public static UserResponse ToResponse(this User user) =>
        new(user.Id, user.UserName, user.Email, user.Roles);
}
