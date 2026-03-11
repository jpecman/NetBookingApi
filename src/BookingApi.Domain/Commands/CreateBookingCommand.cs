namespace BookingApi.Domain.Commands;

public record CreateBookingCommand(
    string Name,
    DateTime From,
    BookingDurationType Duration, // e.g., OneHour, TwoHours
    decimal Price,
    bool IsWholeField,
    Guid ContactId,
    Guid FieldId
    );

public enum BookingDurationType : int
{
    OneHour = 60,
    OneAndHalfHour = 90,
    TwoHours = 120
}
