namespace BookingApi.Application.Commands;

public record UpdateSlotsCommand(DateTime From, DateTime To, IReadOnlyList<SlotUpdate> Slots);

/// <summary>PitchId and BookingId are not written; they guard against a stale client view.</summary>
/// <param name="IsWholeField">
/// Null: only this slot is updated. True: the slot stands for its booking's whole session; slots
/// on the missing pitches are created and Price is the session total, split across the pitches.
/// False: only this slot is kept; the booking's other slots in the session are deleted.
/// </param>
public record SlotUpdate(
    Guid Id,
    // Guid PitchId,
    // Guid BookingId,
    string Name,
    decimal Price,
    bool? IsWholeField = null
);
