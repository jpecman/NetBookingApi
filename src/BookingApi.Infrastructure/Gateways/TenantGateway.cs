using System.Security.Cryptography;

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

public class TenantGateway(BookingApiDbContext dbContext, UserManager<ApplicationUser> userManager)
    : Gateway,
        ITenantGateway
{
    public async Task<Either<Error, Tenant[]>> GetAllAsync()
    {
        try
        {
            var tenants = await dbContext.Tenants.OrderBy(t => t.Name).ToListAsync();
            return Right(tenants.Select(t => t.ToDomain()).ToArray());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<Tenant[]>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, Tenant>> GetAsync(Guid id)
    {
        try
        {
            var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id);

            if (tenant is null)
            {
                return Left(new Error(ErrorType.NotFound, $"Tenant {id} not found."));
            }

            return Right(tenant.ToDomain());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<Tenant>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, Tenant>> CreateAsync(string name)
    {
        Func<string, Task<Tenant>> func = async tenantName =>
        {
            var tenant = new TenantDb { Id = Guid.NewGuid(), Name = tenantName };
            dbContext.Tenants.Add(tenant);
            await dbContext.SaveChangesAsync();

            return tenant.ToDomain();
        };

        return await Execute(func, name);
    }

    public async Task<Either<Error, Tenant>> RenameAsync(Guid id, string name)
    {
        try
        {
            var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id);

            if (tenant is null)
            {
                return Left(new Error(ErrorType.NotFound, $"Tenant {id} not found."));
            }

            tenant.Name = name;
            await dbContext.SaveChangesAsync();

            return Right(tenant.ToDomain());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<Tenant>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, string>> ResetApiKeyAsync(Guid id)
    {
        try
        {
            var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id);

            if (tenant is null)
            {
                return Left(new Error(ErrorType.NotFound, $"Tenant {id} not found."));
            }

            var apiKey = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
            tenant.ApiKeyHash = ApiKeyHasher.Hash(apiKey);
            await dbContext.SaveChangesAsync();

            return Right(apiKey);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<string>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, TenantMember[]>> GetMembersAsync(Guid tenantId)
    {
        try
        {
            if (!await dbContext.Tenants.AnyAsync(t => t.Id == tenantId))
            {
                return Left(new Error(ErrorType.NotFound, $"Tenant {tenantId} not found."));
            }

            var members = await QueryMembersAsync(tenantId);
            return Right(members);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<TenantMember[]>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, TenantInvite>> AddMemberAsync(
        Guid tenantId,
        string email,
        string role
    )
    {
        if (!UserRoles.MembershipRoles.Contains(role))
        {
            return Left(new Error(ErrorType.Invalid, $"Role '{role}' is not assignable."));
        }

        ApplicationUser? user;
        string? temporaryPassword = null;

        try
        {
            if (!await dbContext.Tenants.AnyAsync(t => t.Id == tenantId))
            {
                return Left(new Error(ErrorType.NotFound, $"Tenant {tenantId} not found."));
            }

            user = await userManager.FindByEmailAsync(email);

            if (user is null)
            {
                temporaryPassword = TempPassword.Generate();
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    MustChangePassword = true,
                };
                var result = await userManager.CreateAsync(user, temporaryPassword);

                if (!result.Succeeded)
                {
                    return Left(
                        new Error(
                            ErrorType.Invalid,
                            string.Join("; ", result.Errors.Select(e => e.Description))
                        )
                    );
                }
            }
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<TenantInvite>(ex, ErrorType.General);
        }

        // Execute maps the unique-index violation (user already in a tenant, or
        // already a member) to Conflict.
        Func<ApplicationUser, Task<TenantInvite>> func = async u =>
        {
            dbContext.TenantMemberships.Add(
                new TenantMembershipDb
                {
                    UserId = u.Id,
                    TenantId = tenantId,
                    Role = role,
                }
            );
            await dbContext.SaveChangesAsync();

            return new TenantInvite(
                new TenantMember(u.Id, u.UserName ?? string.Empty, u.Email ?? string.Empty, role),
                temporaryPassword
            );
        };

        return await Execute(func, user);
    }

    public async Task<Either<Error, string>> ResetMemberPasswordAsync(Guid tenantId, string userId)
    {
        try
        {
            var isMember = await dbContext.TenantMemberships.AnyAsync(m =>
                m.TenantId == tenantId && m.UserId == userId
            );
            var user = isMember ? await userManager.FindByIdAsync(userId) : null;

            if (user is null)
            {
                return Left(
                    new Error(
                        ErrorType.NotFound,
                        $"User {userId} is not a member of tenant {tenantId}."
                    )
                );
            }

            var temporaryPassword = TempPassword.Generate();

            await userManager.RemovePasswordAsync(user);
            var result = await userManager.AddPasswordAsync(user, temporaryPassword);

            if (!result.Succeeded)
            {
                return Left(
                    new Error(
                        ErrorType.General,
                        string.Join("; ", result.Errors.Select(e => e.Description))
                    )
                );
            }

            user.MustChangePassword = true;
            await userManager.UpdateAsync(user);

            return Right(temporaryPassword);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<string>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, TenantMember>> SetMemberRoleAsync(
        Guid tenantId,
        string userId,
        string role
    )
    {
        if (!UserRoles.MembershipRoles.Contains(role))
        {
            return Left(new Error(ErrorType.Invalid, $"Role '{role}' is not assignable."));
        }

        try
        {
            var membership = await dbContext.TenantMemberships.FirstOrDefaultAsync(m =>
                m.TenantId == tenantId && m.UserId == userId
            );

            if (membership is null)
            {
                return Left(
                    new Error(
                        ErrorType.NotFound,
                        $"User {userId} is not a member of tenant {tenantId}."
                    )
                );
            }

            membership.Role = role;
            await dbContext.SaveChangesAsync();

            var member = (await QueryMembersAsync(tenantId)).First(m => m.UserId == userId);
            return Right(member);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<TenantMember>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, bool>> RemoveMemberAsync(Guid tenantId, string userId)
    {
        try
        {
            var membership = await dbContext.TenantMemberships.FirstOrDefaultAsync(m =>
                m.TenantId == tenantId && m.UserId == userId
            );

            if (membership is null)
            {
                return Left(
                    new Error(
                        ErrorType.NotFound,
                        $"User {userId} is not a member of tenant {tenantId}."
                    )
                );
            }

            dbContext.TenantMemberships.Remove(membership);
            await dbContext.SaveChangesAsync();

            return Right(true);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<bool>(ex, ErrorType.General);
        }
    }

    private async Task<TenantMember[]> QueryMembersAsync(Guid tenantId)
    {
        var members = await dbContext
            .TenantMemberships.Where(m => m.TenantId == tenantId)
            .Join(
                dbContext.Users,
                m => m.UserId,
                u => u.Id,
                (m, u) => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    m.Role,
                }
            )
            .OrderBy(m => m.Email)
            .ToListAsync();

        return members
            .Select(m => new TenantMember(
                m.Id,
                m.UserName ?? string.Empty,
                m.Email ?? string.Empty,
                m.Role
            ))
            .ToArray();
    }
}
