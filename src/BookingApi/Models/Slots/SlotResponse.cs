using BookingApi.Domain.Entities;
using BookingApi.Models.Slots;

namespace BookingApi.Models.Slots
{
    public record SlotResponse
    (
        Guid Id,
        string Name,
        DateTime From,
        DateTime To,
        decimal Price,
        Guid PitchId,
        SlotStatus Status,
        string? CancellationReason
    );
}

public static class SlotsExtensions
{
    public static SlotResponse ToResponse(this Slot s)
    {
        return new SlotResponse(
            s.Id,
            s.Name,
            s.From,
            s.To,
            s.Price,
            s.PitchId,
            s.Status,
            s.CancellationReason
        );
    }
}
