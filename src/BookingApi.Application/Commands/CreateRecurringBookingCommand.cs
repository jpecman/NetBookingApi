namespace BookingApi.Application.Commands;

public record CreateRecurringBookingCommand
(
    string Name,
    DateTime From,
    int DurationInMinutes,
    decimal Price,
    bool IsWholeField,
    Guid ContactId,
    Guid FieldId,
    DateTime StartDate,
    DateTime EndDate
);
