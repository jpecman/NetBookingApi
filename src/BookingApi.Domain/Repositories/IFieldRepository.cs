using System.Collections.Immutable;

using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Search;

namespace BookingApi.Domain.Repositories
{
    public interface IFieldRepository
    {
        Task<Response<Field>> GetAsync(Guid id);
        Task<Response<ImmutableArray<Field>>> SearchAsync(FieldSearch search);
        Task<Response<Field>> Create(Field entity);
        Task<Response<Field>> Update(Field entity);
        Task<Response> Delete(Guid id);
    }
}
