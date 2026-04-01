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

        public async Task<Either<Error, Field>> Create(Field entity)
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

        public Task<Either<Error, bool>> Delete(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Either<Error, Field>> GetAsync(Guid id)
        {
            Func<Guid, Task<Field>> func = async id =>
            {
                var dbEntity = await _dbContext.Fields.FindAsync(id);

                return dbEntity.ToDomain();
            };

            return Execute(func, id);
        }

        public Task<Either<Error, ImmutableArray<Field>>> SearchAsync(FieldSearch search)
        {
            throw new NotImplementedException();
        }

        public Task<Either<Error, Field>> Update(Field entity)
        {
            throw new NotImplementedException();
        }
    }
}
