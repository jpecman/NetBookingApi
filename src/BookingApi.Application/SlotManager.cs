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
    private readonly ILogger<SlotManager> _logger;

    public SlotManager(ISlotGateway slotGateway, ILogger<SlotManager> logger)
    {
        _slotGateway = slotGateway;
        _logger = logger;
    }

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
                var storedById = stored.ToDictionary(s => s.Slot.Id, s => s.Slot);
                var updated = command.Slots
                    .Select(
                        u =>
                            storedById[u.Id] with
                            {
                                Name = u.Name,
                                From = command.From,
                                To = command.To,
                                Price = u.Price
                            }
                    )
                    .ToList();

                var checkResult = await ValidateAgainstStored(command.Slots, stored)
                    .MatchAsync(
                        _ => CheckAlignmentAsync(stored[0].FieldId, updated),
                        err => Left<Error, Unit>(err)
                    );

                return await checkResult.MatchAsync<Either<Error, ImmutableArray<Slot>>>(
                    async _ => await _slotGateway.UpdateAsync(updated),
                    err => err
                );
            },
            err => err
        );
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

            if (slot.Price < 0)
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

    // Checks the edited slots, in their new positions, against every other active slot on
    // the field on the affected days.
    private async Task<Either<Error, Unit>> CheckAlignmentAsync(
        Guid fieldId,
        IReadOnlyList<Slot> updated
    )
    {
        var days = updated.Select(s => s.From.ToUniversalTime().Date).Distinct().ToList();
        var existingResult = await _slotGateway.GetExistingSlotsForDaysAsync(fieldId, days);

        return existingResult.Bind<Unit>(
            existing =>
            {
                var updatedIds = updated.Select(s => s.Id).ToHashSet();
                var fieldSlots = existing
                    .Where(e => !updatedIds.Contains(e.Id))
                    .Select(e => (e.Id, e.From, e.To))
                    .Concat(
                        updated.Select(
                            s => (s.Id, From: s.From.ToUniversalTime(), To: s.To.ToUniversalTime())
                        )
                    )
                    .ToList();

                foreach (var slot in updated)
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
            }
        );
    }
}
