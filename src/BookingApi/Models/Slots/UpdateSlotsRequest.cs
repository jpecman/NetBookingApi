using BookingApi.Application.Commands;

namespace BookingApi.Models.Slots
{
    public record UpdateSlotsRequest(
        DateTime From,
        DateTime To,
        IReadOnlyList<SlotUpdateRequest> Slots
    );

    /// <summary>
    /// Slots are matched by Id. PitchId and BookingId must match the stored slot (409 otherwise);
    /// they are never changed.
    /// </summary>
    public record SlotUpdateRequest(Guid Id, string Name, decimal Price);

    public static class UpdateSlotsRequestExtensions
    {
        public static UpdateSlotsCommand ToDomain(this UpdateSlotsRequest request)
        {
            return new UpdateSlotsCommand(
                request.From,
                request.To,
                request.Slots.Select(s => new SlotUpdate(s.Id, s.Name, s.Price)).ToList()
            );
        }
    }
}
