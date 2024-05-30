using BookingApi.Common.Messaging;
using BookingApi.Domain;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Domain.Search;
using BookingApi.Infrastructure.DbEntities;
using LanguageExt;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace BookingApi.Infrastructure.Repositories
{
    public class ContactRepository : Repository, IContactRepository
    {
        private readonly ILogger<ContactRepository> _logger;
        private readonly BookingApiDbContext _dbContext;

        public ContactRepository(ILogger<ContactRepository> logger, BookingApiDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<Response<Contact>> Create(Contact entity)
        {
            Func<Contact, Task<Contact>> func = async c =>
            {
                var dbEntity = c.ToEntity();

                var entry = _dbContext.Contacts.Add(dbEntity);
                await _dbContext.SaveChangesAsync();

                return entry.Entity.ToDomain();
            };

            return await Execute(func, entity);
        }

        public Task<Response> Delete(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<Contact>> GetAsync(Guid id)
        {
            Func<Guid, Task<Contact>> func = async cId =>
            {
                var entity = await _dbContext.Contacts.FirstAsync(x => x.Id == cId);

                return entity.ToDomain();
            };

            return await Execute(func, id);
        }

        public Task<Response<ImmutableArray<Contact>>> SearchAsync(ContactSearch search)
        {
            Func<ContactSearch, Task<ImmutableArray<Contact>>> func = async s =>
            {
                var contacts = await _dbContext.Contacts.ToListAsync();

                return contacts.Select(c => c.ToDomain()).ToImmutableArray();
            };

            return Execute(func, search);
        }

        public Task<Response<Contact>> Update(Contact entity)
        {
            throw new NotImplementedException();
        }

        protected async Task<Response<Result>> Execute<Parameters, Result>(Func<Parameters, Task<Result>> func, Parameters prms)
        {
            try
            {
                var result = await func(prms);

                return new Success<Result> { Data = result };
            }
            catch (InvalidOperationException invEx)
            {
                _logger.LogError(invEx.Message);
                return CreateErrorResponse<Result>(ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return CreateErrorResponse<Result>(ErrorType.General);
            }
        }

        protected Response<T> CreateErrorResponse<T>(ErrorType errorType)
        {
            return new Failure<T> { Error = errorType };
        }
    }
}
