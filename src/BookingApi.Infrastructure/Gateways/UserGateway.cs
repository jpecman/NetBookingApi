using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;
using BookingApi.Infrastructure.DbEntities;
using BookingApi.Infrastructure.Tenancy;

using LanguageExt;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Gateways;

// DbContext is used directly for read queries because the Identity API (UserManager)
// only supports single-entity operations.
public class UserGateway(
    UserManager<ApplicationUser> userManager,
    BookingApiDbContext dbContext,
    ICurrentTenant currentTenant
) : Gateway, IUserGateway
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
            // The admin chose this password, so the user must replace it at first login.
            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                MustChangePassword = true,
            };
            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var message = string.Join("; ", result.Errors.Select(e => e.Description));
                return Left(new Error(ErrorType.Invalid, message));
            }

            dbContext.TenantMemberships.Add(
                new TenantMembershipDb
                {
                    UserId = user.Id,
                    TenantId = currentTenant.TenantId,
                    Role = UserRoles.Content,
                }
            );
            await dbContext.SaveChangesAsync();

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
            var isMember = await dbContext.TenantMemberships.AnyAsync(m =>
                m.UserId == id && m.TenantId == currentTenant.TenantId
            );
            var user = isMember ? await userManager.FindByIdAsync(id) : null;

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

    // Scoped to the acting tenant via the membership join; the membership role
    // is the user's single effective role.
    private async Task<User[]> QueryUsersAsync(IQueryable<ApplicationUser> query)
    {
        var data = await query
            .Join(
                dbContext.TenantMemberships.Where(m => m.TenantId == currentTenant.TenantId),
                u => u.Id,
                m => m.UserId,
                (u, m) => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    m.Role,
                }
            )
            .ToListAsync();

        return data.Select(u => new User(u.Id, u.UserName ?? string.Empty, u.Email ?? string.Empty, [u.Role])).ToArray();
    }
}
