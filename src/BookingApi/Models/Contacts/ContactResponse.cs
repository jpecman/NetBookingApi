using BookingApi.Domain.Entities;

namespace BookingApi.Models.Contacts
{
    public record ContactResponse(Guid Id, string FirstName, string LastName, string Email, string Phone, bool Show);

    public static class ContactExtensions
    {
        public static ContactResponse ToResponse(this Contact contact)
        {
            return new ContactResponse(contact.Id, contact.FirstName, contact.LastName, contact.Email, contact.Phone, contact.Show);
        }
    }
    
}
