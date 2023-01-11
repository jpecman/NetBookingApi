using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public record ContactSearch(string FirstName, string LastName, string Email, string Phone);
}
