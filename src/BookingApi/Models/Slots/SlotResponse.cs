using BookingApi.Domain.Entities;
using BookingApi.Models.Fields;
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
        FieldResponse Field
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
                    s.Field.ToResponse()
                    );
    }
}
