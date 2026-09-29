using BookingApi.Application.Commands;

namespace BookingApi.Models.Bookings;

/// <summary>
/// Renaming a booking whose slots were given custom names fails with 409 unless
/// <see cref="OverwriteSlotNames"/> is set.
/// </summary>
public record UpdateBookingRequest(string Name, Guid ContactId, bool OverwriteSlotNames = false);

public static class UpdateBookingRequestExtensions
{
    public static UpdateBookingCommand ToDomain(this UpdateBookingRequest request, Guid id)
    {
        return new UpdateBookingCommand(
            id,
            request.Name,
            request.ContactId,
            request.OverwriteSlotNames
        );
    }
}
