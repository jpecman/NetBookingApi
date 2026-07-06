namespace BookingApi.Domain.Entities;

public enum SlotStatus
{
    Booked = 0,
    Invoiced = 1,
    Paid = 2,
    // Value 3 is hardcoded in the EnsureUniqueSlot exclusion constraint migration — do not change it
    Cancelled = 3
}
