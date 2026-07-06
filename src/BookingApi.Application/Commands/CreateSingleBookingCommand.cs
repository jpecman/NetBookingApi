namespace BookingApi.Application.Commands;

public record CreateSingleBookingCommand(
    string Name,
    DateTime From,
    BookingDurationType Duration,
    decimal Price,
    bool IsWholeField,
    Guid ContactId,
    Guid FieldId,
    DateTime? EndDate = null
    );

public enum BookingDurationType : int
{
    OneHour = 60,
    OneAndHalfHour = 90,
    TwoHours = 120
}
