using BookingApi.Domain.Entities;
using BookingApi.Models.Contacts;
using BookingApi.Models.Slots;

namespace BookingApi.Models.Bookings;

public record BookingResponse(
    Guid Id,
    string Name,
    ContactResponse Contact,
    SlotResponse[] Slots,
    BookingType Type
);

public static class BookingExtensions
{
    public static BookingResponse ToResponse(this Booking b)
    {
        return new BookingResponse(
            b.Id,
            b.Name,
            b.Contact.ToResponse(),
            b.Slots.Select(slot => slot.ToResponse()).ToArray(),
            b.Type
            );
    }
}
