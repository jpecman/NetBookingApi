using System.Collections.Immutable;

using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Search;

using LanguageExt;

namespace BookingApi.Domain.Gateways;

public interface ISlotGateway
{
    Task<Either<Error, ImmutableArray<Slot>>> GetAsync(SlotSearch search);

    Task<
        Either<Error, IReadOnlyList<(Guid Id, Guid PitchId, DateTime From, DateTime To)>>
    > GetExistingSlotsForDaysAsync(Guid fieldId, IReadOnlyList<DateTime> sessionDates);

    /// <summary>Returns NotFound if any of the ids doesn't exist.</summary>
    Task<Either<Error, ImmutableArray<(Slot Slot, Guid FieldId)>>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids
    );

    /// <summary>The booking's active slots with exactly this time range.</summary>
    Task<Either<Error, ImmutableArray<Slot>>> GetSessionSlotsAsync(
        Guid bookingId,
        DateTime from,
        DateTime to
    );

    Task<Either<Error, Unit>> CancelAsync(Guid slotId, string? cancellationReason);

    /// <summary>
    /// In one transaction: writes name, time range and price of every updated slot, inserts the
    /// added slots (each into its BookingId) and deletes the given slots. Returns the updated
    /// and added slots.
    /// </summary>
    Task<Either<Error, ImmutableArray<Slot>>> UpdateAsync(
        IReadOnlyList<Slot> updated,
        IReadOnlyList<Slot> added,
        IReadOnlyCollection<Guid> deletedIds
    );
}
