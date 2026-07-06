using System.Collections.Immutable;

using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;
using BookingApi.Domain.Search;
using BookingApi.Infrastructure.DbEntities;

using LanguageExt;

using Microsoft.EntityFrameworkCore;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Gateways;

public class SlotGateway : Gateway, ISlotGateway
{
    private readonly BookingApiDbContext _dbContext;

    public SlotGateway(BookingApiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Either<Error, Unit>> CancelAsync(Guid slotId, string? cancellationReason)
    {
        try
        {
            var slot = await _dbContext.Slots.FindAsync(slotId);

            if (slot is null)
                return Left(new Error(ErrorType.NotFound, $"Slot {slotId} not found"));

            slot.Status = SlotStatus.Cancelled;
            slot.CancellationReason = cancellationReason;

            await _dbContext.SaveChangesAsync();
            return Right(Unit.Default);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<Unit>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, ImmutableArray<Slot>>> GetAsync(SlotSearch search)
    {
        try
        {
            var slots = await _dbContext.Slots.Include(s => s.Pitch)
                .Where(s => s.Duration.LowerBound >= search.From.ToUniversalTime() && s.Duration.UpperBound <= search.To.ToUniversalTime())
                .ToListAsync();

            return Right(ImmutableArray.Create(slots.Select(x => x.ToDomain()).ToArray()));
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<ImmutableArray<Slot>>(ex, ErrorType.General);
        }
    }
}
