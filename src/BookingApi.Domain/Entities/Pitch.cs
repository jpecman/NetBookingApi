using System.Collections.Immutable;

namespace BookingApi.Domain.Entities;

public record Pitch(Guid Id, string Name, Guid FieldId, ImmutableArray<Slot> Slots);
