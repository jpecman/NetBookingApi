using System.Collections.Immutable;

using BookingApi.Domain.Entities;

namespace BookingApi.Models.Fields
{
    public record CreateFieldRequest(string Name, IReadOnlyList<string> Pitches);

    public static class CreateFieldRequestExtensions
    {
        public static Field ToDomain(this CreateFieldRequest request)
        {
            var pitches = request.Pitches
                .Select(name => new Pitch(Guid.Empty, name, Guid.Empty, ImmutableArray<Slot>.Empty))
                .ToImmutableArray();

            return new Field(Guid.Empty, request.Name, pitches);
        }
    }

    public record UpdateFieldRequest(string Name);

    public static class UpdateFieldRequestExtensions
    {
        public static Field ToDomain(this UpdateFieldRequest request, Guid id) =>
            new(id, request.Name, []);
    }
}
