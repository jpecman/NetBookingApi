
using BookingApi.Domain.Entities;
using BookingApi.Models.Contacts;
using BookingApi.Models.Fields;

namespace BookingApi.Models.Bookings;

public record BookingRequest(
    string Name,
    DateTime From,
    DateTime To,
    decimal Price,
    bool IsWholeField,
    ContactIdRequest Contact,
    FieldIdRequest Field
);

public static class BookingRequestExtensions
{
    public static Booking ToDomain(this BookingRequest request)
    {
        return new Booking
        (
            Guid.Empty,
            request.Name,
            request.Contact.ToDomain(),
            null
        );
    }
}
