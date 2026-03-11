
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
    public static CreateBookingCommand ToDomain(this BookingRequest request)
    {
        var listBuilder = ImmutableList.CreateBuilder<Slot>();

        var to = request.GetBookingEndTime();

        listBuilder.Add(new Slot(Guid.Empty, request.Name, request.From, to, request.Price, request.Field.ToDomain()));

        var booking = new Booking
        (
            Guid.Empty,
            request.Name,
            request.Contact.ToDomain(),
            listBuilder.ToImmutable()
        );

        return booking;
    }

    public static DateTime GetBookingEndTime(this BookingRequest request)
    {
        return request.From.AddHours(request.Duration.GetEventLength());
    }

    private static double GetEventLength(this BookingDurationType bt)
    {
        if (bt == BookingDurationType.TwoHours)
        {
            return 2; // 2 hours for a match
        }
        else if (bt == BookingDurationType.OneAndHalfHour)
        {
            return 1.5;
        }
        else if (bt == BookingDurationType.OneHour)
        {
            return 1;
        }
        else
        {
            return 1.5;
        }
    }
}
