using System.Collections.Immutable;
using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;

using LanguageExt;

using static LanguageExt.Prelude;

namespace BookingApi.Application;

/// <summary>
/// Turns booking sessions into slots on a field's pitches.
/// </summary>
public static class SlotAllocation
{
    // The session price is split across the pitches so the slot prices sum to it; the
    // rounding remainder goes to the first pitch.
    public static ImmutableList<Slot> AllocateWholeField(
        Field field,
        IReadOnlyList<(DateTime From, DateTime To, decimal Price)> sessions,
        string name
    ) =>
        sessions
            .SelectMany(
                session =>
                {
                    var share = Math.Round(session.Price / field.Pitches.Length, 2);
                    var remainder = session.Price - share * field.Pitches.Length;

                    return field.Pitches.Select(
                        (pitch, index) =>
                            new Slot(
                                Guid.NewGuid(),
                                name,
                                session.From,
                                session.To,
                                index == 0 ? share + remainder : share,
                                pitch.Id
                            )
                    );
                }
            )
            .ToImmutableList();

    // Each session goes to the first pitch that is free, given the existing slots on the
    // field on the session days.
    public static Either<Error, ImmutableList<Slot>> AllocateHalfField(
        Field field,
        IReadOnlyList<(DateTime From, DateTime To, decimal Price)> sessions,
        string name,
        IEnumerable<(Guid Id, Guid PitchId, DateTime From, DateTime To)> existing
    )
    {
        // Existing slots plus the ones built so far, so sessions of the same request
        // see each other.
        var taken = existing.Select(c => (c.PitchId, c.From, c.To)).ToList();
        var slots = new List<Slot>();
        foreach (var session in sessions)
        {
            var sessionFrom = session.From.ToUniversalTime();
            var sessionTo = session.To.ToUniversalTime();

            var overlapping = taken.Where(c => c.From < sessionTo && c.To > sessionFrom).ToList();

            if (
                SlotAlignment.IsMisaligned(
                    sessionFrom,
                    sessionTo,
                    overlapping.Select(c => (c.From, c.To))
                )
            )
                return Left<Error, ImmutableList<Slot>>(
                    new Error(
                        ErrorType.Conflict,
                        $"Half-field booking at {session.From:g} must have the same start and end as the existing booking on this field"
                    )
                );

            var conflicted = overlapping.Select(c => c.PitchId).ToHashSet();
            var pitch = field.Pitches.FirstOrDefault(p => !conflicted.Contains(p.Id));
            if (pitch is null)
                return Left<Error, ImmutableList<Slot>>(
                    new Error(
                        ErrorType.Conflict,
                        $"No available pitch for session at {session.From:g}"
                    )
                );

            slots.Add(
                new Slot(Guid.NewGuid(), name, session.From, session.To, session.Price, pitch.Id)
            );
            taken.Add((pitch.Id, sessionFrom, sessionTo));
        }
        return Right<Error, ImmutableList<Slot>>(slots.ToImmutableList());
    }
}
