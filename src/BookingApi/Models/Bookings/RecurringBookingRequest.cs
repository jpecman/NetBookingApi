using System.Collections.Immutable;

using BookingApi.Domain.Entities;
using BookingApi.Models.Contacts;
using BookingApi.Models.Fields;

namespace BookingApi.Models.Bookings
{
    public record RecurringBookingRequest
    (
        string Name,
        DateTime From,
        BookingDurationType DurationType,
        decimal Price,
        bool IsWholeField,
        ContactIdRequest Contact,
        FieldIdRequest Field,
        DateTime StartDate,
        DateTime EndDate
    ) : BookingRequest(Name, From, DurationType, Price, IsWholeField, Contact, Field);

    public static class RecurringBookingRequestExtensions
    {
        public static Booking ToDomain(this RecurringBookingRequest request)
        {
            var currentDate = request.StartDate.Date;

            var listBuilder = ImmutableList.CreateBuilder<Slot>();

            while(currentDate <= request.EndDate.Date)
            {
                // TODO: add validation that currentdate is the same date as the from and to dates?
                var from = currentDate.Date.Add(request.From.TimeOfDay);
                var to = currentDate.Date.Add(request.GetBookingEndTime().TimeOfDay);

                listBuilder.Add(new Slot(Guid.Empty, request.Name, from, to, request.Price, request.Field.ToDomain()));

                currentDate = currentDate.AddDays(7);
            }

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
}
