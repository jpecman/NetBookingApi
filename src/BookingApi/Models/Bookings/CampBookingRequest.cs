using BookingApi.Application.Commands;

namespace BookingApi.Models.Bookings;

public record CampBookingRequest(
    string Name,
    bool IsWholeField,
    Guid ContactId,
    Guid FieldId,
    IReadOnlyList<SessionRequest> Sessions
);

public record SessionRequest(DateTime From, int Duration, decimal Price);

public static class CampBookingRequestExtensions
{
    public static CreateCampBookingCommand ToDomain(this CampBookingRequest request)
    {
        return new CreateCampBookingCommand(
            request.Name,
            request.IsWholeField,
            request.ContactId,
            request.FieldId,
            request.Sessions.Select(s => new CampSession(s.From, s.Duration, s.Price)).ToList()
        );
    }
}
