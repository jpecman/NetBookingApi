using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;

using LanguageExt;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Gateways;

public class UserGateway(UserManager<ApplicationUser> userManager) : Gateway, IUserGateway
{
    public async Task<Either<Error, User[]>> GetAllAsync()
    {
        try
        {
            var users = await userManager.Users.ToListAsync();
            return Right(users.Select(ToDomain).ToArray());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<User[]>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, User>> GetAsync(string id)
    {
        try
        {
            var user = await userManager.FindByIdAsync(id);

            if (user is null)
            {
                return Left(new Error(ErrorType.NotFound, $"User {id} not found."));
            }

            return Right(ToDomain(user));
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<User>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, User>> CreateAsync(string userName, string email, string password)
    {
        try
        {
            var user = new ApplicationUser { UserName = userName, Email = email };
            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var message = string.Join("; ", result.Errors.Select(e => e.Description));
                return Left(new Error(ErrorType.Invalid, message));
            }

            return Right(ToDomain(user));
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<User>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, bool>> DeleteAsync(string id)
    {
        try
        {
            var user = await userManager.FindByIdAsync(id);

            if (user is null)
            {
                return Left(new Error(ErrorType.NotFound, $"User {id} not found."));
            }

            var result = await userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                var message = string.Join("; ", result.Errors.Select(e => e.Description));
                return Left(new Error(ErrorType.Invalid, message));
            }

            return Right(true);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<bool>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, string[]>> GetRolesAsync(string userId)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return Left(new Error(ErrorType.NotFound, $"User {userId} not found."));
            }

            var roles = await userManager.GetRolesAsync(user);
            return Right(roles.ToArray());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<string[]>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, bool>> AddToRoleAsync(string userId, string roleName)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return Left(new Error(ErrorType.NotFound, $"User {userId} not found."));
            }

            var result = await userManager.AddToRoleAsync(user, roleName);

            if (!result.Succeeded)
            {
                var message = string.Join("; ", result.Errors.Select(e => e.Description));
                return Left(new Error(ErrorType.Invalid, message));
            }

            return Right(true);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<bool>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, bool>> RemoveFromRoleAsync(string userId, string roleName)
    {
        try
        {
            var user = await userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return Left(new Error(ErrorType.NotFound, $"User {userId} not found."));
            }

            var result = await userManager.RemoveFromRoleAsync(user, roleName);

            if (!result.Succeeded)
            {
                var message = string.Join("; ", result.Errors.Select(e => e.Description));
                return Left(new Error(ErrorType.Invalid, message));
            }

            return Right(true);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<bool>(ex, ErrorType.General);
        }
    }

    private static User ToDomain(ApplicationUser user) =>
        new(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty);
}
