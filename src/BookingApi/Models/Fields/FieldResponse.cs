using BookingApi.Domain.Entities;

namespace BookingApi.Models.Fields
{
    public record PitchResponse(Guid Id, string Name);

    public record FieldResponse(Guid Id, string Name, IReadOnlyList<PitchResponse> Pitches);

    public static class FieldExtensions
    {
        public static FieldResponse ToResponse(this Field f)
        {
            var pitches = f.Pitches.Select(p => new PitchResponse(p.Id, p.Name)).ToArray();
            return new FieldResponse(f.Id, f.Name, pitches);
        }
    }
}
