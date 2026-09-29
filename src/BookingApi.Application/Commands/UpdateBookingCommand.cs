namespace BookingApi.Application.Commands;

public record UpdateBookingCommand(
    Guid Id,
    string Name,
    Guid ContactId,
    bool OverwriteSlotNames = false
);
