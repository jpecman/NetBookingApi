using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;

using LanguageExt;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Gateways;

public class RoleGateway(RoleManager<IdentityRole> roleManager) : Gateway, IRoleGateway
{
    public async Task<Either<Error, Role[]>> GetAllAsync()
    {
        try
        {
            var roles = await roleManager.Roles.ToListAsync();
            return Right(roles.Select(ToDomain).ToArray());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<Role[]>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, Role>> CreateAsync(string name)
    {
        try
        {
            var role = new IdentityRole(name);
            var result = await roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                var message = string.Join("; ", result.Errors.Select(e => e.Description));
                return Left(new Error(ErrorType.Invalid, message));
            }

            return Right(ToDomain(role));
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<Role>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, bool>> DeleteAsync(string id)
    {
        try
        {
            var role = await roleManager.FindByIdAsync(id);

            if (role is null)
            {
                return Left(new Error(ErrorType.NotFound, $"Role {id} not found."));
            }

            var result = await roleManager.DeleteAsync(role);

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

    private static Role ToDomain(IdentityRole role) =>
        new(role.Id, role.Name ?? string.Empty);
}
