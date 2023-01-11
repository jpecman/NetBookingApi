
using BookingApi.Domain.Entities;

namespace BookingApi.Models.Bookings;

public record BookingRequest(
    string Name,
    DateTime From,
    DateTime To,
    decimal Price,
    bool IsWholeField
);

public static class BookingRequestExtensions
{
    public static Booking ToDomain(this BookingRequest request)
    {
        return new Booking
        (
            Guid.Empty,
            request.Name,
            DateOnly.FromDateTime(request.From.Date),
            TimeOnly.FromDateTime(request.From),
            TimeOnly.FromDateTime(request.To),
            request.Price
        );
    }
}