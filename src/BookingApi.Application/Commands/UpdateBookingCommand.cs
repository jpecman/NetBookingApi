namespace BookingApi.Application.Commands;

public record UpdateBookingCommand(Guid Id, string Name, Guid ContactId);

public record UpdateSessionCommand(
    Guid BookingId,
    DateTime CurrentFrom,
    DateTime CurrentTo,
    DateTime NewFrom,
    DateTime NewTo,
    decimal Price
);
