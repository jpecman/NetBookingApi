namespace BookingApi.Application.Commands;

public record CreateCampBookingCommand(
    string Name,
    bool IsWholeField,
    Guid ContactId,
    Guid FieldId,
    IReadOnlyList<CampSession> Sessions
);

public record CampSession(DateTime From, int Duration, decimal Price);
