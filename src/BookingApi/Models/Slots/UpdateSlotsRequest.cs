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
    /// IsWholeField changes the session's field size (see SlotUpdate). When true, Price is the
    /// session total; otherwise it is this slot's price.
    /// </summary>
    public record SlotUpdateRequest(Guid Id, string Name, decimal Price, bool? IsWholeField = null);

    public static class UpdateSlotsRequestExtensions
    {
        public static UpdateSlotsCommand ToDomain(this UpdateSlotsRequest request)
        {
            return new UpdateSlotsCommand(
                request.From,
                request.To,
                request
                    .Slots.Select(s => new SlotUpdate(s.Id, s.Name, s.Price, s.IsWholeField))
                    .ToList()
            );
        }
    }
}
