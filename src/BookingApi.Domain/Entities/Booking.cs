namespace BookingApi.Domain.Entities;

public record Booking
(
    Guid Id,
    string Name,
    DateOnly Date,
    TimeOnly From,
    TimeOnly To,
    decimal Price
);