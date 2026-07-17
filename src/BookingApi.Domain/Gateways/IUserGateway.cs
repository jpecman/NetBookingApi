using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;

using LanguageExt;

namespace BookingApi.Domain.Gateways;

public interface IUserGateway
{
    Task<Either<Error, User[]>> GetAllAsync();
    Task<Either<Error, User>> GetAsync(string id);
    Task<Either<Error, User>> CreateAsync(string userName, string email, string password);
    Task<Either<Error, bool>> DeleteAsync(string id);
    Task<Either<Error, string[]>> GetRolesAsync(string userId);
    Task<Either<Error, bool>> AddToRoleAsync(string userId, string roleName);
    Task<Either<Error, bool>> RemoveFromRoleAsync(string userId, string roleName);
}
