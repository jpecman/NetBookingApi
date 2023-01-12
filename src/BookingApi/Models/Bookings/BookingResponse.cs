using BookingApi.Domain.Entities;

namespace BookingApi.Models.Bookings;

public record BookingResponse(
    Guid Id,
    string Name,
    DateTime From,
    DateTime To,
    decimal Price,
    bool IsWholeField
);

public static class BookingExtensions
{
    public static BookingResponse ToResponse(this Booking b)
    {
        return new BookingResponse(b.Id, b.Name, b.Date.ToDateTime(b.From), b.Date.ToDateTime(b.To), b.Price, true);
    }
}