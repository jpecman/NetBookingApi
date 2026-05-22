using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;
using BookingApi.Infrastructure.DbEntities;

using LanguageExt;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Gateways;

public class PitchGateway : Gateway, IPitchGateway
{
    private readonly ILogger<PitchGateway> _logger;
    private readonly BookingApiDbContext _dbContext;

    public PitchGateway(ILogger<PitchGateway> logger, BookingApiDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Either<Error, Pitch>> CreateAsync(Guid fieldId, string name)
    {
        try
        {
            var field = await _dbContext.Fields.FirstOrDefaultAsync(f => f.Id == fieldId);

            if (field is null)
                return Left(new Error(ErrorType.NotFound, $"Field {fieldId} not found."));

            var entity = new PitchDb { Id = Guid.NewGuid(), Name = name, FieldId = fieldId };
            _dbContext.Pitches.Add(entity);
            await _dbContext.SaveChangesAsync();

            return Right(entity.ToDomain());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<Pitch>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, Pitch>> UpdateAsync(Guid id, string name)
    {
        try
        {
            var entity = await _dbContext.Pitches.FirstOrDefaultAsync(p => p.Id == id);

            if (entity is null)
                return Left(new Error(ErrorType.NotFound, $"Pitch {id} not found."));

            entity.Name = name;
            await _dbContext.SaveChangesAsync();

            return Right(entity.ToDomain());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<Pitch>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, bool>> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await _dbContext.Pitches.FirstOrDefaultAsync(p => p.Id == id);

            if (entity is null)
                return Left(new Error(ErrorType.NotFound, $"Pitch {id} not found."));

            _dbContext.Pitches.Remove(entity);
            await _dbContext.SaveChangesAsync();

            return Right(true);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<bool>(ex, ErrorType.General);
        }
    }
}
