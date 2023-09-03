using BookingApi.Domain.Entities;

namespace BookingApi.Models.Fields
{
    public record FieldResponse
    (
        Guid Id,
        string Name
    );

    public static class FieldExtensions
    {
        public static FieldResponse ToResponse(this Field f)
        {
            return new FieldResponse(f.Id, f.Name);
        }
    }
}
