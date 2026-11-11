using System.Collections.Immutable;
using System.Diagnostics;

using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;
using BookingApi.Domain.Search;
using BookingApi.Infrastructure.DbEntities;

using LanguageExt;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NpgsqlTypes;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Gateways;

public class SlotGateway : Gateway, ISlotGateway
{
    private readonly BookingApiDbContext _dbContext;
    private readonly ILogger<SlotGateway> _logger;

    public SlotGateway(BookingApiDbContext dbContext, ILogger<SlotGateway> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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
        Either<Error, IReadOnlyList<(Guid Id, Guid PitchId, DateTime From, DateTime To)>>
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
                            s.Id,
                            s.PitchId,
                            From = s.Duration.LowerBound,
                            To = s.Duration.UpperBound
                        }
                )
                .ToListAsync();

            return Right<Error, IReadOnlyList<(Guid, Guid, DateTime, DateTime)>>(
                candidates.Select(c => (c.Id, c.PitchId, c.From, c.To)).ToList()
            );
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<IReadOnlyList<(Guid, Guid, DateTime, DateTime)>>(
                ex,
                ErrorType.General
            );
        }
    }

    public async Task<Either<Error, ImmutableArray<Slot>>> GetAsync(SlotSearch search)
    {
        if (RangeLimits.CheckSearch(search.From, search.To) is { } rangeError)
            return rangeError;

        try
        {
            var stopwatch = Stopwatch.StartNew();
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

            _logger.LogInformation(
                "Loaded {SlotCount} slots for field {FieldId} range {From}-{To} in {ElapsedMs}ms",
                slots.Count,
                search.FieldId,
                search.From,
                search.To,
                stopwatch.ElapsedMilliseconds
            );

            return Right(ImmutableArray.Create(slots.Select(x => x.ToDomain()).ToArray()));
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<ImmutableArray<Slot>>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, ImmutableArray<(Slot Slot, Guid FieldId)>>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids
    )
    {
        try
        {
            var entities = await _dbContext.Slots
                .Include(s => s.Pitch)
                .Where(s => ids.Contains(s.Id))
                .ToListAsync();

            var missing = ids.Except(entities.Select(e => e.Id)).ToList();
            if (missing.Count > 0)
                return Left(
                    new Error(
                        ErrorType.NotFound,
                        $"Slots not found: {string.Join(", ", missing)}"
                    )
                );

            return Right(entities.Select(e => (e.ToDomain(), e.Pitch.FieldId)).ToImmutableArray());
        }
        catch (Exception ex)
        {
            return CreateErrorResponse<ImmutableArray<(Slot, Guid)>>(ex, ErrorType.General);
        }
    }

    public async Task<Either<Error, ImmutableArray<Slot>>> UpdateAsync(IReadOnlyList<Slot> slots)
    {
        Func<IReadOnlyList<Slot>, Task<ImmutableArray<Slot>>> func = async updates =>
        {
            var ids = updates.Select(s => s.Id).ToList();
            var entities = await _dbContext.Slots
                .Where(s => ids.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            foreach (var slot in updates)
            {
                if (!entities.TryGetValue(slot.Id, out var entity))
                    throw new InvalidOperationException($"Slot {slot.Id} not found.");

                entity.Name = slot.Name;
                entity.Duration = new NpgsqlRange<DateTime>(
                    slot.From.ToUniversalTime(),
                    lowerBoundIsInclusive: true,
                    slot.To.ToUniversalTime(),
                    upperBoundIsInclusive: false
                );
                entity.Price = slot.Price;
            }

            // One SaveChanges = one transaction; overlaps with other slots on the same pitch
            // surface as an exclusion violation, which Execute maps to Conflict.
            await _dbContext.SaveChangesAsync();

            return updates.Select(s => entities[s.Id].ToDomain()).ToImmutableArray();
        };

        return await Execute(func, slots);
    }
}
