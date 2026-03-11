using System.Collections.Immutable;

using BookingApi.Domain.Entities;
using BookingApi.Models.Bookings;

namespace BookingApi.Models.Fields
{
    public record CreateFieldRequest(string Name);

    public static class CreateFieldRequestExtensions
    {
        public static Field ToDomain(this CreateFieldRequest request)
        {
            return new Field(Guid.Empty, request.Name, new ImmutableArray<Pitch>());
        }
    }
}
