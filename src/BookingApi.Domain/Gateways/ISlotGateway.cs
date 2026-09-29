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

    Task<Either<Error, Unit>> CancelAsync(Guid slotId, string? cancellationReason);

    /// <summary>Writes name, time range and price of every slot in one transaction.</summary>
    Task<Either<Error, ImmutableArray<Slot>>> UpdateAsync(IReadOnlyList<Slot> slots);
}
