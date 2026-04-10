using BookingApi.Domain.Entities;

namespace BookingApi.Models.Contacts
{
    public record CreateContactRequest(string FirstName, string LastName, string Email, string Phone);

    public static class CreateContactRequestExtensions
    {
        public static Contact ToDomain(this CreateContactRequest request)
        {
            return new Contact(Guid.Empty, request.FirstName, request.LastName, request.Email, request.Phone);
        }
    }

    public record UpdateContactRequest(string FirstName, string LastName, string Email, string Phone);

    public static class UpdateContactRequestExtensions
    {
        public static Contact ToDomain(this UpdateContactRequest request, Guid id)
        {
            return new Contact(id, request.FirstName, request.LastName, request.Email, request.Phone);
        }
    }
}
