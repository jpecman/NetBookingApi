using BookingApi.Domain.Entities;

namespace BookingApi.Models.Contacts
{
    public record ContactIdRequest(Guid Id);

    public static class ContactIdRequestExtensions
    {
        public static Contact ToDomain(this ContactIdRequest request)
        {
            return Contact.EmptyWithId(request.Id);
        }
    }
}
