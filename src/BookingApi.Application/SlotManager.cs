using System.Collections.Immutable;
using BookingApi.Application.Commands;
using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace BookingApi.Application;

public interface ISlotManager
{
    /// <summary>
    /// Updates a set of slots all-or-nothing: either every slot is written, or none is.
    /// </summary>
    Task<Either<Error, ImmutableArray<Slot>>> UpdateAsync(UpdateSlotsCommand command);
}

public class SlotManager : ISlotManager
{
    private readonly ISlotGateway _slotGateway;
    private readonly IFieldGateway _fieldGateway;
    private readonly ILogger<SlotManager> _logger;

    public SlotManager(
        ISlotGateway slotGateway,
        IFieldGateway fieldGateway,
        ILogger<SlotManager> logger
    )
    {
        _slotGateway = slotGateway;
        _fieldGateway = fieldGateway;
        _logger = logger;
    }

    // What one PATCH writes: existing slots with their new values, new slots, and slots to delete.
    private sealed record SlotChanges(List<Slot> Updated, List<Slot> Added, List<Guid> DeletedIds);

    public async Task<Either<Error, ImmutableArray<Slot>>> UpdateAsync(UpdateSlotsCommand command)
    {
        var ids = command.Slots.Select(s => s.Id).ToList();

        var result = await ValidateInput(command.Slots)
            .MatchAsync<Either<Error, ImmutableArray<Slot>>>(
                async _ => await UpdateValidatedAsync(command, ids),
                err => err
            );

        result.IfRight(
            slots =>
                _logger.LogInformation(
                    "Updated {SlotCount} slots: {SlotIds}",
                    slots.Length,
                    string.Join(", ", ids)
                )
        );
        result.IfLeft(
            err =>
                _logger.LogWarning(
                    "Failed to update slots {SlotIds}: {Error}",
                    string.Join(", ", ids),
                    err.Message
                )
        );

        return result;
    }

    private async Task<Either<Error, ImmutableArray<Slot>>> UpdateValidatedAsync(
        UpdateSlotsCommand command,
        IReadOnlyCollection<Guid> ids
    )
    {
        var storedResult = await _slotGateway.GetByIdsAsync(ids);

        return await storedResult.MatchAsync<Either<Error, ImmutableArray<Slot>>>(
            async stored =>
            {
                var changesResult = await ValidateAgainstStored(command.Slots, stored)
                    .MatchAsync(
                        _ => PlanChangesAsync(command, stored),
                        err => Left<Error, SlotChanges>(err)
                    );

                var checkResult = await changesResult.MatchAsync(
                    async changes =>
                        (await CheckAlignmentAsync(stored[0].FieldId, changes)).Map(_ => changes),
                    err => Left<Error, SlotChanges>(err)
                );

                return await checkResult.MatchAsync<Either<Error, ImmutableArray<Slot>>>(
                    async changes =>
                        await _slotGateway.UpdateAsync(
                            changes.Updated,
                            changes.Added,
                            changes.DeletedIds
                        ),
                    err => err
                );
            },
            err => err
        );
    }

    // Entries without IsWholeField update just their slot. With the flag, the entry stands for
    // its booking's session: true makes it one slot per pitch (creating the missing ones, price
    // split as on creation), false keeps only this slot and deletes the session's other slots.
    private async Task<Either<Error, SlotChanges>> PlanChangesAsync(
        UpdateSlotsCommand command,
        ImmutableArray<(Slot Slot, Guid FieldId)> stored
    )
    {
        var storedById = stored.ToDictionary(s => s.Slot.Id, s => s.Slot);
        var requestedIds = command.Slots.Select(s => s.Id).ToHashSet();
        var changes = new SlotChanges([], [], []);
        Field? field = null;

        foreach (var update in command.Slots)
        {
            var original = storedById[update.Id];
            var slot = original with
            {
                Name = update.Name,
                From = command.From,
                To = command.To,
                Price = update.Price
            };

            if (update.IsWholeField is null)
            {
                changes.Updated.Add(slot);
                continue;
            }

            var sessionResult = await _slotGateway.GetSessionSlotsAsync(
                original.BookingId,
                original.From,
                original.To
            );
            if (sessionResult.IsLeft)
                return sessionResult.LeftToSeq().Head;

            var siblings = sessionResult.RightToSeq().Head.Where(s => s.Id != update.Id).ToList();
            if (siblings.Any(s => requestedIds.Contains(s.Id)))
                return new Error(
                    ErrorType.Invalid,
                    $"Slot {update.Id}: with isWholeField set, no other slot of its session may be given"
                );

            if (update.IsWholeField == false)
            {
                changes.Updated.Add(slot);
                changes.DeletedIds.AddRange(siblings.Select(s => s.Id));
                continue;
            }

            if (field is null)
            {
                var fieldResult = await _fieldGateway.GetAsync(stored[0].FieldId);
                if (fieldResult.IsLeft)
                    return fieldResult.LeftToSeq().Head;
                field = fieldResult.RightToSeq().Head;
            }

            var sessionByPitch = siblings.Append(original).ToDictionary(s => s.PitchId);
            var wholeSlots = SlotAllocation.AllocateWholeField(
                field,
                [(command.From, command.To, update.Price)],
                update.Name
            );

            foreach (var whole in wholeSlots)
            {
                if (sessionByPitch.TryGetValue(whole.PitchId, out var existing))
                    changes.Updated.Add(
                        existing with
                        {
                            Name = whole.Name,
                            From = whole.From,
                            To = whole.To,
                            Price = whole.Price
                        }
                    );
                else
                    changes.Added.Add(whole with { BookingId = original.BookingId });
            }
        }

        return changes;
    }

    private static Either<Error, Unit> ValidateInput(IReadOnlyList<SlotUpdate> slots)
    {
        if (slots.Count == 0)
            return new Error(ErrorType.Invalid, "At least one slot must be given");

        if (slots.Select(s => s.Id).Distinct().Count() != slots.Count)
            return new Error(ErrorType.Invalid, "Each slot may be given only once");

        foreach (var slot in slots)
        {
            // TODO: check this
            // if (slot.From >= slot.To)
            //     return new Error(
            //         ErrorType.Invalid,
            //         $"Slot {slot.Id}: start must be before its end"
            //     );

            if (slot.Price < 0) //TODO: there should be a check that the price isn't higher than say 10000
                return new Error(ErrorType.Invalid, $"Slot {slot.Id}: price must not be negative");

            if (string.IsNullOrWhiteSpace(slot.Name))
                return new Error(ErrorType.Invalid, $"Slot {slot.Id}: name must not be empty");
        }

        return unit;
    }

    private static Either<Error, Unit> ValidateAgainstStored(
        IReadOnlyList<SlotUpdate> updates,
        ImmutableArray<(Slot Slot, Guid FieldId)> stored
    )
    {
        var storedById = stored.ToDictionary(s => s.Slot.Id, s => s.Slot);

        foreach (var update in updates)
        {
            var slot = storedById[update.Id];

            // TODO: check this
            // The client's view of the slot is out of date.
            // if (slot.PitchId != update.PitchId || slot.BookingId != update.BookingId)
            //     return new Error(
            //         ErrorType.Conflict,
            //         $"Slot {update.Id} no longer matches its pitch or booking; reload and try again"
            //     );

            if (slot.Status == SlotStatus.Cancelled)
                return new Error(ErrorType.Invalid, $"Slot {update.Id} is cancelled");
        }

        if (stored.Select(s => s.FieldId).Distinct().Count() > 1)
            return new Error(ErrorType.Invalid, "All slots must be on the same field");

        return unit;
    }

    // Checks the edited and added slots, in their new positions, against every other active
    // slot on the field on the affected days (deleted slots no longer count).
    private async Task<Either<Error, Unit>> CheckAlignmentAsync(Guid fieldId, SlotChanges changes)
    {
        var changed = changes.Updated.Concat(changes.Added).ToList();
        var days = changed.Select(s => s.From.ToUniversalTime().Date).Distinct().ToList();
        var existingResult = await _slotGateway.GetExistingSlotsForDaysAsync(fieldId, days);

        return existingResult.Bind<Unit>(existing =>
        {
            var replacedIds = changes.Updated
                .Select(s => s.Id)
                .Concat(changes.DeletedIds)
                .ToHashSet();
            var unchanged = existing.Where(e => !replacedIds.Contains(e.Id)).ToList();

            // A new slot needs its pitch free; give a clear error before the database would.
            foreach (var slot in changes.Added)
            {
                var from = slot.From.ToUniversalTime();
                var to = slot.To.ToUniversalTime();

                if (unchanged.Any(o => o.PitchId == slot.PitchId && o.From < to && o.To > from))
                    return new Error(
                        ErrorType.Conflict,
                        $"The rest of the field is not free at {slot.From:g}"
                    );
            }

            var fieldSlots = unchanged
                .Select(e => (e.Id, e.From, e.To))
                .Concat(
                    changed.Select(
                        s => (s.Id, From: s.From.ToUniversalTime(), To: s.To.ToUniversalTime())
                    )
                )
                .ToList();

            foreach (var slot in changed)
            {
                var from = slot.From.ToUniversalTime();
                var to = slot.To.ToUniversalTime();
                var others = fieldSlots.Where(o => o.Id != slot.Id).Select(o => (o.From, o.To));

                if (SlotAlignment.IsMisaligned(from, to, others))
                    return new Error(
                        ErrorType.Conflict,
                        $"Slot at {slot.From:g} must have the same start and end as the slots it overlaps on this field"
                    );
            }

            return unit;
        });
    }
}
