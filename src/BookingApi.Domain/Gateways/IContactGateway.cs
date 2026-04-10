using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;

using LanguageExt;

namespace BookingApi.Domain.Gateways;

public interface IContactGateway
{
    Task<Either<Error, Contact[]>> GetAllAsync();
    Task<Either<Error, Contact>> GetAsync(Guid id);
    Task<Either<Error, Contact>> CreateAsync(Contact contact);
    Task<Either<Error, Contact>> UpdateAsync(Contact contact);
    Task<Either<Error, bool>> DeleteAsync(Guid id);
}
