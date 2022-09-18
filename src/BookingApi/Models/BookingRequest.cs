
namespace BookingApi.Models;

public record BookingRequest(
    string Name,
    DateTime From,
    DateTime To,
    decimal Price,
    bool IsWholeField
);