using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;
using BookingApi.Domain.Search;
using BookingApi.Infrastructure.DbEntities;

using LanguageExt;

using static LanguageExt.Prelude;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System.Collections.Immutable;

namespace BookingApi.Infrastructure.Gateways
{
    public class FieldGateway : Gateway, IFieldGateway
    {
        private readonly ILogger<FieldGateway> _logger;
        private readonly BookingApiDbContext _dbContext;

        public FieldGateway(ILogger<FieldGateway> logger, BookingApiDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<Either<Error, Field>> Create(Field entity)
        {
            Func<Field, Task<Field>> func = async f =>
            {
                var fieldId = Guid.NewGuid();
                var dbEntity = new FieldDb
                {
                    Id = fieldId,
                    Name = f.Name,
                    Pitches = f.Pitches
                        .Select(p => new PitchDb { Id = Guid.NewGuid(), Name = p.Name, FieldId = fieldId })
                        .ToList()
                };

                var entry = _dbContext.Fields.Add(dbEntity);
                await _dbContext.SaveChangesAsync();

                return entry.Entity.ToDomain();
            };

            return await Execute(func, entity);
        }

        public async Task<Either<Error, bool>> Delete(Guid id)
        {
            try
            {
                var entity = await _dbContext.Fields.FirstOrDefaultAsync(f => f.Id == id);

                if (entity is null)
                {
                    return Left(new Error(ErrorType.NotFound, $"Field {id} not found."));
                }

                _dbContext.Fields.Remove(entity);
                await _dbContext.SaveChangesAsync();

                return Right(true);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<bool>(ex, ErrorType.General);
            }
        }

        public async Task<Either<Error, ImmutableArray<Field>>> GetAllAsync()
        {
            try
            {
                var entities = await _dbContext.Fields.Include(f => f.Pitches).ToListAsync();
                ImmutableArray<Field> fields = [..entities.Select(e => e.ToDomain())];
                return Right(fields);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<ImmutableArray<Field>>(ex, ErrorType.General);
            }
        }

        public async Task<Either<Error, Field>> GetAsync(Guid id)
        {
            try
            {
                var entity = await _dbContext.Fields.Include(f => f.Pitches).FirstOrDefaultAsync(f => f.Id == id);

                if (entity is null)
                {
                    return Left(new Error(ErrorType.NotFound, $"Field {id} not found."));
                }

                return Right(entity.ToDomain());
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<Field>(ex, ErrorType.General);
            }
        }

        public Task<Either<Error, ImmutableArray<Field>>> SearchAsync(FieldSearch search)
        {
            throw new NotImplementedException();
        }

        public async Task<Either<Error, Field>> Update(Field entity)
        {
            try
            {
                var dbEntity = await _dbContext.Fields.Include(f => f.Pitches).FirstOrDefaultAsync(f => f.Id == entity.Id);

                if (dbEntity is null)
                {
                    return Left(new Error(ErrorType.NotFound, $"Field {entity.Id} not found."));
                }

                dbEntity.Name = entity.Name;
                await _dbContext.SaveChangesAsync();

                return Right(dbEntity.ToDomain());
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<Field>(ex, ErrorType.General);
            }
        }
    }
}
