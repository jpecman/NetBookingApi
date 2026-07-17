using BookingApi.Domain.Search;

namespace BookingApi.Models.Slots;

public record SlotRequest(DateTime From, DateTime To, Guid FieldId);

public static class SlotRequestExtensions
{
    public static SlotSearch ToDomain(this SlotRequest request) =>
        new(request.From, request.To, request.FieldId);
}
