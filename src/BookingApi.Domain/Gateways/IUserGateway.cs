using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;

using LanguageExt;

namespace BookingApi.Domain.Gateways;

// Tenant-admin surface: operates on users of the acting tenant only. Role
// management happens through tenant memberships (ITenantGateway).
public interface IUserGateway
{
    Task<Either<Error, User[]>> GetAllAsync();
    Task<Either<Error, User>> GetAsync(string id);
    Task<Either<Error, User>> CreateAsync(string userName, string email, string password);
    // Refuses self-deletion and deleting a SuperAdmin.
    Task<Either<Error, bool>> DeleteAsync(string id, string actingUserId);
}
