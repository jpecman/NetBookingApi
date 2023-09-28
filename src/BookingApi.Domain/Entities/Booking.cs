using System.Collections.Immutable;

namespace BookingApi.Domain.Entities;

public record Booking
(
    Guid Id,
    string Name,
    Contact Contact,
    ImmutableList<Slot> Slots
)
{
    public DateTime From => Slots.Min(x => x.From);

    public DateTime To => Slots.Max(x => x.To);
}
