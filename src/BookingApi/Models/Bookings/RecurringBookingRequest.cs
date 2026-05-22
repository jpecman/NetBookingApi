using System.Collections.Immutable;

using BookingApi.Application.Commands;
using BookingApi.Domain.Entities;
using BookingApi.Models.Contacts;
using BookingApi.Models.Fields;

namespace BookingApi.Models.Bookings
{
    public record RecurringBookingRequest
    (
        string Name,
        DateTime From,
        int DurationInMinutes,
        decimal Price,
        bool IsWholeField,
        Guid ContactId,
        Guid FieldId,
        DateTime StartDate,
        DateTime EndDate
    );

    public static class RecurringBookingRequestExtensions
    {
        public static CreateRecurringBookingCommand ToDomain(this RecurringBookingRequest request)
        {
            return new CreateRecurringBookingCommand
            (
                request.Name,
                request.From,
                request.DurationInMinutes,
                request.Price,
                request.IsWholeField,
                request.ContactId,
                request.FieldId,
                request.StartDate,
                request.EndDate
            );
        }
    }
}
