using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;

using LanguageExt;

namespace BookingApi.Domain.Gateways;

public interface IRoleGateway
{
    Task<Either<Error, Role[]>> GetAllAsync();
    Task<Either<Error, Role>> CreateAsync(string name);
    Task<Either<Error, bool>> DeleteAsync(string id);
}
