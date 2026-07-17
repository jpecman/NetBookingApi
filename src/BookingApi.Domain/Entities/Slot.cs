using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Domain.Entities
{
    public record Slot(
        Guid Id,
        string Name,
        DateTime From,
        DateTime To,
        decimal Price,
        Guid PitchId,
        SlotStatus Status = SlotStatus.Booked,
        string? CancellationReason = null
    );
}
