namespace BookingApi.Domain.Entities;

public record ContactBookingSummary(
    Contact Contact,
    int BookingCount,
    decimal TotalPrice,
    DateTime EarliestBookingDate,
    DateTime LatestBookingDate,
    SlotStatus Status
);
