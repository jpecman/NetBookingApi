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

namespace BookingApi.Infrastructure.Repositories
{
    public class FieldRepository : Repository, IFieldRepository
    {
        private readonly ILogger<FieldRepository> _logger;
        private readonly BookingApiDbContext _dbContext;

        public FieldRepository(ILogger<FieldRepository> logger, BookingApiDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<Response<Field>> Create(Field entity)
        {
            Func<Field, Task<Field>> func = async f =>
            {
                var dbEntity = f.ToEntity();

                var entry = _dbContext.Fields.Add(dbEntity);
                await _dbContext.SaveChangesAsync();

                return entry.Entity.ToDomain();
            };

            return await Execute(func, entity);
        }

        public Task<Response> Delete(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Response<Field>> GetAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Response<ImmutableArray<Field>>> SearchAsync(FieldSearch search)
        {
            throw new NotImplementedException();
        }

        public Task<Response<Field>> Update(Field entity)
        {
            throw new NotImplementedException();
        }

        protected async Task<Response<Result>> Execute<Parameters, Result>(
            Func<Parameters, Task<Result>> func,
            Parameters prms
        )
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
