using BookingApi.Domain.Entities;
using BookingApi.Models.Fields;

namespace BookingApi.Models.Pitches;

public record CreatePitchRequest(Guid FieldId, string Name);

public record UpdatePitchRequest(string Name);

public static class PitchModelExtensions
{
    public static PitchResponse ToResponse(this Pitch pitch) => new(pitch.Id, pitch.Name);
}
