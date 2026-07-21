using BookingApi.Application.Commands;

namespace BookingApi.Models.Bookings;

public record UpdateBookingRequest(string Name, Guid ContactId);

public record UpdateSessionRequest(
    DateTime CurrentFrom,
    DateTime CurrentTo,
    DateTime NewFrom,
    DateTime NewTo,
    decimal Price
);

public static class UpdateBookingRequestExtensions
{
    public static UpdateBookingCommand ToDomain(this UpdateBookingRequest request, Guid id)
    {
        return new UpdateBookingCommand(id, request.Name, request.ContactId);
    }

    public static UpdateSessionCommand ToDomain(this UpdateSessionRequest request, Guid bookingId)
    {
        return new UpdateSessionCommand(
            bookingId,
            request.CurrentFrom,
            request.CurrentTo,
            request.NewFrom,
            request.NewTo,
            request.Price
        );
    }
}
