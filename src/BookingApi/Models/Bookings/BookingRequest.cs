
using System.Collections.Immutable;

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
        var listBuilder = ImmutableList.CreateBuilder<Slot>();
        listBuilder.Add(new Slot(Guid.Empty, request.Name, request.From, request.To, request.Price, request.Field.ToDomain()));

        var booking = new Booking
        (
            Guid.Empty,
            request.Name,
            request.Contact.ToDomain(),
            listBuilder.ToImmutable()
        );

        return booking;
    }
}
