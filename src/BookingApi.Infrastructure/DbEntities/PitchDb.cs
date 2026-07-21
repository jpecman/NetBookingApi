using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using BookingApi.Domain.Entities;
using BookingApi.Infrastructure.Tenancy;

namespace BookingApi.Infrastructure.DbEntities;

public class PitchDb : ITenantOwned
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public string Name { get; set; } = null!;

    [Required]
    public Guid FieldId { get; set; }

    [ForeignKey(nameof(FieldId))]
    public FieldDb Field { get; set; } = null!;

    public ICollection<SlotDb>? Slots { get; set; }
}

public static class PitchExtensions
{
    public static Pitch ToDomain(this PitchDb pitch)
    {
        var slots = pitch.Slots != null
            ? [..pitch.Slots.Select(x => x.ToDomain())]
            : ImmutableArray<Slot>.Empty;
        return new Pitch(pitch.Id, pitch.Name, pitch.FieldId, slots);
    }

    public static PitchDb ToEntity(this Pitch pitch)
    {
        return new PitchDb { Id = pitch.Id, Name = pitch.Name, FieldId = pitch.FieldId };
    }
}
