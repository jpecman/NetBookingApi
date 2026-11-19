namespace BookingApi.Domain.Entities;

public enum BookingType
{
    // Values are hardcoded in the AddBookingType backfill migration — do not change them
    Unknown = 0,
    Single = 1,
    Repeated = 2,
    Camp = 3
}
