namespace BookingApi.Domain.Entities;

public record Booking
(
    Guid Id,
    string Name,
    DateTime From,
    DateTime To,
    decimal Price
);