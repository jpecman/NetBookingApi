using System.Collections.Immutable;

using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Search;

namespace BookingApi.Domain.Repositories
{
    public interface IContactRepository
    {
        Task<Response<Contact>> GetAsync(Guid id);
        Task<Response<ImmutableArray<Contact>>> SearchAsync(ContactSearch search);
        Task<Response<Contact>> Create(Contact entity);
        Task<Response<Contact>> Update(Contact entity);
        Task<Response> Delete(Guid id);
    }
}
