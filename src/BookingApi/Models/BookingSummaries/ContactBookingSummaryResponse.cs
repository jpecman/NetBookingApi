using BookingApi.Domain.Entities;
using BookingApi.Models.Contacts;

namespace BookingApi.Models.BookingSummaries
{
    public record ContactBookingSummaryResponse(
        ContactResponse Contact,
        int BookingCount,
        decimal TotalPrice,
        DateTime EarliestBookingDate,
        DateTime LatestBookingDate,
        SlotStatus Status
    );

    public static class ContactBookingSummaryExtensions
    {
        public static ContactBookingSummaryResponse ToResponse(this ContactBookingSummary summary)
        {
            return new ContactBookingSummaryResponse(
                summary.Contact.ToResponse(),
                summary.BookingCount,
                summary.TotalPrice,
                summary.EarliestBookingDate,
                summary.LatestBookingDate,
                summary.Status
            );
        }
    }
}
