using BookingApi.Domain.Entities;

namespace BookingApi.Models.Fields
{
    public record FieldIdRequest(Guid Id);

    public static class FieldIdRequestExtensions
    {
        public static Field ToDomain(this FieldIdRequest request)
        {
            return Field.EmptyWithId(request.Id);
        }
    }
}
