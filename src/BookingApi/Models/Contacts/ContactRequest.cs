using BookingApi.Domain.Entities;
using BookingApi.Models.Bookings;

namespace BookingApi.Models.Contacts
{
    public record ContactRequest(string FirstName, string LastName, string Email, string Phone);

    public static class ContactRequestExtensions
    {
        public static Contact ToDomain(this ContactRequest request)
        {
            return new Contact
            (
                Guid.Empty,
                request.FirstName,
                request.LastName,
                request.Email,
                request.Phone
            );
        }
    }
}
