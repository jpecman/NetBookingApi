using System.Collections.Immutable;
using BookingApi.Application.Commands;
using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;
using BookingApi.Domain.Search;

using LanguageExt;

using static LanguageExt.Prelude;

namespace BookingApi.Application;

public interface IBookingManager
{
    Task<Either<Error, Booking>> CreateAsync(CreateSingleBookingCommand command);

    Task<Either<Error, Booking>> CreateCampAsync(CreateCampBookingCommand command);

    Task<Either<Error, Booking>> GetAsync(Guid id);

    Task<Either<Error, Booking[]>> GetAsync(BookingSearch search);

    Task<Either<Error, ContactBookingSummary[]>> GetGroupedByContactAsync(BookingSearch search);

    Task<Either<Error, Unit>> CancelBookingAsync(Guid id, string? cancellationReason);
}

public class BookingManager : IBookingManager
{
    private readonly IBookingGateway _bookingGateway;
    private readonly IFieldGateway _fieldGateway;
    private readonly ISlotGateway _slotGateway;

    public BookingManager(
        IBookingGateway bookingGateway,
        IFieldGateway fieldGateway,
        ISlotGateway slotGateway
    )
    {
        _bookingGateway = bookingGateway;
        _fieldGateway = fieldGateway;
        _slotGateway = slotGateway;
    }

    public async Task<Either<Error, Booking>> CreateAsync(CreateSingleBookingCommand command)
    {
        var fieldResult = await _fieldGateway.GetAsync(command.FieldId);

        return await fieldResult.MatchAsync<Either<Error, Booking>>(
            async field =>
            {
                var sessions = GenerateWeeklySessions(command)
                    .Select(s => (s.From, s.To, Price: command.Price))
                    .ToList();

                var slotsResult = command.IsWholeField
                    ? Right<Error, ImmutableList<Slot>>(
                        BuildWholeFieldSlots(field, sessions, command.Name)
                    )
                    : await BuildHalfFieldSlotsAsync(field, sessions, command.Name);

                return await slotsResult.MatchAsync<Either<Error, Booking>>(
                    async slots =>
                    {
                        var booking = new Booking(
                            Guid.NewGuid(),
                            command.Name,
                            Contact.EmptyWithId(command.ContactId),
                            slots
                        );
                        var newBooking = await _bookingGateway.CreateAsync(booking);
                        return newBooking.Match<Either<Error, Booking>>(b => b, err => err);
                    },
                    err => err
                );
            },
            err => err
        );
    }

    public async Task<Either<Error, Booking>> CreateCampAsync(CreateCampBookingCommand command)
    {
        var fieldResult = await _fieldGateway.GetAsync(command.FieldId);

        return await fieldResult.MatchAsync<Either<Error, Booking>>(
            async field =>
            {
                var sessions = command.Sessions
                    .Select(s => (s.From, To: s.From.AddMinutes(s.Duration), Price: s.Price))
                    .ToList();

                var slotsResult = command.IsWholeField
                    ? Right<Error, ImmutableList<Slot>>(
                        BuildWholeFieldSlots(field, sessions, command.Name)
                    )
                    : await BuildHalfFieldSlotsAsync(field, sessions, command.Name);

                return await slotsResult.MatchAsync<Either<Error, Booking>>(
                    async slots =>
                    {
                        var booking = new Booking(
                            Guid.NewGuid(),
                            command.Name,
                            Contact.EmptyWithId(command.ContactId),
                            slots
                        );
                        var newBooking = await _bookingGateway.CreateAsync(booking);
                        return newBooking.Match<Either<Error, Booking>>(b => b, err => err);
                    },
                    err => err
                );
            },
            err => err
        );
    }

    public Task<Either<Error, Unit>> CancelBookingAsync(Guid id, string? cancellationReason)
    {
        return _bookingGateway.CancelAsync(id, cancellationReason);
    }

    public Task<Either<Error, Booking>> GetAsync(Guid id)
    {
        return _bookingGateway.GetAsync(id);
    }

    public async Task<Either<Error, Booking[]>> GetAsync(BookingSearch search)
    {
        var response = await _bookingGateway.GetAsync(search);

        return response.Match<Either<Error, Booking[]>>(
            bookings => bookings.ToArray(),
            error => error
        );
    }

    public async Task<Either<Error, ContactBookingSummary[]>> GetGroupedByContactAsync(
        BookingSearch search
    )
    {
        var response = await _bookingGateway.GetAsync(search with { IncludeCancelled = false });

        return response.Match<Either<Error, ContactBookingSummary[]>>(
            bookings =>
                bookings
                    .GroupBy(b => b.Contact)
                    .Select(
                        g =>
                            new ContactBookingSummary(
                                g.Key,
                                g.Count(),
                                g.Sum(b => b.Slots.Sum(s => s.Price)),
                                g.Min(b => b.From),
                                g.Max(b => b.To),
                                g.Min(b => b.Slots.Min(s => s.Status))
                            )
                    )
                    .ToArray(),
            error => error
        );
    }

    private static ImmutableList<Slot> BuildWholeFieldSlots(
        Field field,
        IReadOnlyList<(DateTime From, DateTime To, decimal Price)> sessions,
        string name
    ) =>
        sessions
            .SelectMany(
                session =>
                    field.Pitches.Select(
                        pitch =>
                            new Slot(
                                Guid.NewGuid(),
                                name,
                                session.From,
                                session.To,
                                session.Price,
                                pitch.Id
                            )
                    )
            )
            .ToImmutableList();

    private async Task<Either<Error, ImmutableList<Slot>>> BuildHalfFieldSlotsAsync(
        Field field,
        IReadOnlyList<(DateTime From, DateTime To, decimal Price)> sessions,
        string name
    )
    {
        var sessionDates = sessions.Select(s => s.From.ToUniversalTime().Date).Distinct().ToList();

        var candidatesResult = await _slotGateway.GetExistingSlotsForDaysAsync(
            field.Id,
            sessionDates
        );

        return candidatesResult.Match<Either<Error, ImmutableList<Slot>>>(
            candidates =>
            {
                var slots = new List<Slot>();
                foreach (var session in sessions)
                {
                    var sessionFrom = session.From.ToUniversalTime();
                    var sessionTo = session.To.ToUniversalTime();

                    var overlapping = candidates
                        .Where(c => c.From < sessionTo && c.To > sessionFrom)
                        .ToList();

                    var misaligned = overlapping.Any(
                        c => c.From != sessionFrom || c.To != sessionTo
                    );

                    if (misaligned)
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
                        new Slot(
                            Guid.NewGuid(),
                            name,
                            session.From,
                            session.To,
                            session.Price,
                            pitch.Id
                        )
                    );
                }
                return Right<Error, ImmutableList<Slot>>(slots.ToImmutableList());
            },
            err => err
        );
    }

    private static IEnumerable<(DateTime From, DateTime To)> GenerateWeeklySessions(
        CreateSingleBookingCommand command
    )
    {
        var durationMinutes = (int)command.Duration;

        if (command.EndDate is null)
        {
            yield return (command.From, command.From.AddMinutes(durationMinutes));
            yield break;
        }

        var current = command.From;
        while (current <= command.EndDate)
        {
            yield return (current, current.AddMinutes(durationMinutes));
            current = current.AddDays(7);
        }
    }
}
