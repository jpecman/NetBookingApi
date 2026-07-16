using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;

using LanguageExt;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Gateways;

// DbContext is used directly for read queries because the Identity API (UserManager, RoleManager)
// only supports single-entity operations, which leads to N+1 queries when loading users with roles.
public class UserGateway(UserManager<ApplicationUser> userManager, BookingApiDbContext dbContext) : Gateway, IUserGateway
{
    public async Task<Either<Error, User[]>> GetAllAsync()
    {
        try
        {
            return Right(await QueryUsersAsync(dbContext.Users));
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
            var user = (await QueryUsersAsync(dbContext.Users.Where(u => u.Id == id))).FirstOrDefault();

            if (user is null)
            {
                return Left(new Error(ErrorType.NotFound, $"User {id} not found."));
            }

            return Right(user);
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

            return Right(new User(user.Id, user.UserName ?? string.Empty, user.Email ?? string.Empty, []));
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

    private async Task<User[]> QueryUsersAsync(IQueryable<ApplicationUser> query)
    {
        var data = await query
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                Roles = dbContext.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                    .ToArray()
            })
            .ToListAsync();

        return data.Select(u => new User(u.Id, u.UserName ?? string.Empty, u.Email ?? string.Empty, u.Roles)).ToArray();
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
}
