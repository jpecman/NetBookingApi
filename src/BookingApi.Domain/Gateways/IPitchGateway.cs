using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;

using LanguageExt;

namespace BookingApi.Domain.Gateways;

public interface IPitchGateway
{
    Task<Either<Error, Pitch>> CreateAsync(Guid fieldId, string name);
    Task<Either<Error, Pitch>> UpdateAsync(Guid id, string name);
    Task<Either<Error, bool>> DeleteAsync(Guid id);
}
