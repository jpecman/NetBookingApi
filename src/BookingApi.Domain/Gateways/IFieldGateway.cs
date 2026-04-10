using System.Collections.Immutable;

using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Search;

using LanguageExt;

namespace BookingApi.Domain.Gateways
{
    public interface IFieldGateway
    {
        Task<Either<Error, ImmutableArray<Field>>> GetAllAsync();
        Task<Either<Error, Field>> GetAsync(Guid id);
        Task<Either<Error, ImmutableArray<Field>>> SearchAsync(FieldSearch search);
        Task<Either<Error, Field>> Create(Field entity);
        Task<Either<Error, Field>> Update(Field entity);
        Task<Either<Error, bool>> Delete(Guid id);
    }
}
