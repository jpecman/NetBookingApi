
using BookingApi.Domain.Entities;

namespace BookingApi.Models;

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
        return new Booking(Guid.Empty, request.Name, request.From, request.To, request.Price);
    }
}