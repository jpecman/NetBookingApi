namespace BookingApi.Application.Commands;

public record UpdateSlotsCommand(DateTime From, DateTime To, IReadOnlyList<SlotUpdate> Slots);

/// <summary>PitchId and BookingId are not written; they guard against a stale client view.</summary>
public record SlotUpdate(
    Guid Id,
    // Guid PitchId,
    // Guid BookingId,
    string Name,
    decimal Price
);
