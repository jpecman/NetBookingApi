
using System.Collections.Immutable;

using BookingApi.Domain.Commands;
using BookingApi.Domain.Entities;
using BookingApi.Models.Contacts;
using BookingApi.Models.Fields;

namespace BookingApi.Models.Bookings;

public record BookingRequest(
    string Name,
    DateTime From,
    int Duration,
    decimal Price,
    bool IsWholeField,
    Guid ContactId,
    Guid FieldId
);

public static class BookingRequestExtensions
{
    public static CreateSingleBookingCommand ToDomain(this BookingRequest request)
    {
        return new CreateSingleBookingCommand(
            request.Name,
            request.From,
            (BookingDurationType)request.Duration, // TODO: Check this
            request.Price,
            request.IsWholeField,
            request.ContactId,
            request.FieldId
        );
    }
}
