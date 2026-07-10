using BookingApi.Domain.Search;

namespace BookingApi.Models.Bookings
{
    public class SearchBookingRequest
    {
        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public bool IncludeCancelled { get; set; } = false;
    }

    public static class SearchBookingRequestExtensions
    {
        public static BookingSearch ToDomain(this SearchBookingRequest request)
        {
            return new BookingSearch(
                request.From,
                request.To,
                Guid.Empty,
                request.IncludeCancelled
            );
        }
    }
}
