using BookingApi.Domain.Search;

namespace BookingApi.Models.BookingSummaries
{
    public class ContactBookingSummaryRequest
    {
        public DateTime From { get; set; }

        public DateTime To { get; set; }
    }

    public static class ContactBookingSummaryRequestExtensions
    {
        public static BookingSearch ToDomain(this ContactBookingSummaryRequest request)
        {
            return new BookingSearch(request.From, request.To, Guid.Empty, IncludeCancelled: false);
        }
    }
}
