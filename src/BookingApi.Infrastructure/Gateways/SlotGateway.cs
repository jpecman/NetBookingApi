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

    public async Task<
        Either<Error, IReadOnlyList<(Guid PitchId, DateTime From, DateTime To)>>
    > GetExistingSlotsForDaysAsync(Guid fieldId, IReadOnlyList<DateTime> sessionDates)
    {
        try
        {
            var candidates = await _dbContext.Slots
                .Where(
                    s =>
                        s.Pitch.FieldId == fieldId
                        && s.Status != SlotStatus.Cancelled
                        && sessionDates.Contains(s.Duration.LowerBound.Date)
                )
                .Select(
                    s =>
                        new
                        {
                            s.PitchId,
                            From = s.Duration.LowerBound,
                            To = s.Duration.UpperBound
                        }
                )
                .ToListAsync();

            return Right<Error, IReadOnlyList<(Guid, DateTime, DateTime)>>(
                candidates.Select(c => (c.PitchId, c.From, c.To)).ToList()
            );
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<IReadOnlyList<(Guid, DateTime, DateTime)>>(
                ex,
                ErrorType.General
            );
        }
    }

    public async Task<Either<Error, ImmutableArray<Slot>>> GetAsync(SlotSearch search)
    {
        try
        {
            var slots = await _dbContext.Slots
                .Include(s => s.Pitch)
                .Where(
                    s =>
                        s.Pitch.FieldId == search.FieldId
                        && s.Duration.LowerBound >= search.From.ToUniversalTime()
                        && s.Duration.UpperBound <= search.To.ToUniversalTime()
                        && (search.IncludeCancelled || s.Status != SlotStatus.Cancelled)
                )
                .ToListAsync();

            return Right(ImmutableArray.Create(slots.Select(x => x.ToDomain()).ToArray()));
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<ImmutableArray<Slot>>(ex, ErrorType.General);
        }
    }
}
