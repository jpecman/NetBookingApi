using BookingApi.Domain.Entities;

namespace BookingApi.Models.Bookings;

public record BookingResponse(
    Guid Id,
    string Name,
    bool IsWholeField
);

public static class BookingExtensions
{
    public static BookingResponse ToResponse(this Booking b)
    {
        return new BookingResponse(b.Id, b.Name, true);
    }
}