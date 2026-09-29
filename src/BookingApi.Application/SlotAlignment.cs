namespace BookingApi.Application;

/// <summary>
/// Field alignment invariant: slots on the same field that overlap in time must share the
/// exact same start and end, so a field is only ever split into whole sessions.
/// </summary>
public static class SlotAlignment
{
    public static bool IsMisaligned(
        DateTime from,
        DateTime to,
        IEnumerable<(DateTime From, DateTime To)> others
    ) => others.Any(o => o.From < to && o.To > from && (o.From != from || o.To != to));
}
