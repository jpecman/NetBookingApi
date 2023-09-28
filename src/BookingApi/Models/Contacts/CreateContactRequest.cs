using BookingApi.Domain.Entities;
using BookingApi.Models.Bookings;

namespace BookingApi.Models.Contacts
{
    public record CreateContactRequest(string FirstName, string LastName, string Email, string Phone);

    public static class CreateContactRequestExtensions
    {
        public static Contact ToDomain(this CreateContactRequest request)
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
