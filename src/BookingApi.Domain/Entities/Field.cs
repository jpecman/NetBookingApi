using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Domain.Entities
{
    public record Field
    (
        Guid Id,
        string Name,
        ImmutableArray<Pitch> Pitches
    )
    {
        public static Field EmptyWithId(Guid id) => new Field(id, string.Empty, new ImmutableArray<Pitch>());
    }
}
