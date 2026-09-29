using System.Collections.Immutable;
using BookingApi.Application.Commands;
using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;
using BookingApi.Domain.Search;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace BookingApi.Application;

public interface IBookingManager
{
    Task<Either<Error, Booking>> CreateAsync(CreateSingleBookingCommand command);

    Task<Either<Error, Booking[]>> CreateCampAsync(CreateCampBookingCommand command);

    Task<Either<Error, Booking>> UpdateAsync(UpdateBookingCommand command);

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
    private readonly ILogger<BookingManager> _logger;

    public BookingManager(
        IBookingGateway bookingGateway,
        IFieldGateway fieldGateway,
        ISlotGateway slotGateway,
        ILogger<BookingManager> logger
    )
    {
        _bookingGateway = bookingGateway;
        _fieldGateway = fieldGateway;
        _slotGateway = slotGateway;
        _logger = logger;
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
                        SlotAllocation.AllocateWholeField(field, sessions, command.Name)
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

                        newBooking.IfRight(
                            b =>
                                _logger.LogInformation(
                                    "Created booking {BookingId} for contact {ContactId} on field {FieldId} with {SlotCount} slots",
                                    b.Id,
                                    command.ContactId,
                                    command.FieldId,
                                    slots.Count
                                )
                        );
                        newBooking.IfLeft(
                            err =>
                                _logger.LogWarning(
                                    "Failed to persist booking for contact {ContactId} on field {FieldId}: {Error}",
                                    command.ContactId,
                                    command.FieldId,
                                    err.Message
                                )
                        );

                        return newBooking.Match<Either<Error, Booking>>(b => b, err => err);
                    },
                    err => err
                );
            },
            err =>
            {
                _logger.LogWarning(
                    "Field {FieldId} not found for booking creation: {Error}",
                    command.FieldId,
                    err.Message
                );
                return err;
            }
        );
    }

    // Creates one booking per week until EndDate. Weeks that conflict with existing bookings
    // are skipped; any other error stops the loop (weeks created so far stay persisted).
    public async Task<Either<Error, Booking[]>> CreateCampAsync(CreateCampBookingCommand command)
    {
        var fieldResult = await _fieldGateway.GetAsync(command.FieldId);

        return await fieldResult.MatchAsync<Either<Error, Booking[]>>(
            async field =>
            {
                var created = new List<Booking>();
                var skipped = new List<Error>();

                foreach (var sessions in GenerateCampWeeks(command))
                {
                    var weekResult = await CreateCampWeekAsync(field, command, sessions);

                    if (weekResult.IsRight)
                    {
                        created.Add(weekResult.RightToSeq().Head);
                        continue;
                    }

                    var error = weekResult.LeftToSeq().Head;
                    if (error.ErrorType != ErrorType.Conflict)
                        return error;

                    skipped.Add(error);
                    _logger.LogWarning(
                        "Skipped camp week starting {WeekStart} for contact {ContactId} on field {FieldId}: {Error}",
                        sessions.Min(s => s.From),
                        command.ContactId,
                        command.FieldId,
                        error.Message
                    );
                }

                _logger.LogInformation(
                    "Camp booking for contact {ContactId} on field {FieldId}: {CreatedCount} weeks created, {SkippedCount} skipped",
                    command.ContactId,
                    command.FieldId,
                    created.Count,
                    skipped.Count
                );

                // Every week conflicted; report the first week's reason.
                if (created.Count == 0)
                    return skipped[0];

                return created.ToArray();
            },
            err =>
            {
                _logger.LogWarning(
                    "Field {FieldId} not found for camp booking creation: {Error}",
                    command.FieldId,
                    err.Message
                );
                return err;
            }
        );
    }

    private async Task<Either<Error, Booking>> CreateCampWeekAsync(
        Field field,
        CreateCampBookingCommand command,
        List<(DateTime From, DateTime To, decimal Price)> sessions
    )
    {
        var slotsResult = command.IsWholeField
            ? Right<Error, ImmutableList<Slot>>(
                SlotAllocation.AllocateWholeField(field, sessions, command.Name)
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

                newBooking.IfRight(
                    b =>
                        _logger.LogInformation(
                            "Created camp booking {BookingId} for contact {ContactId} on field {FieldId} with {SlotCount} slots",
                            b.Id,
                            command.ContactId,
                            command.FieldId,
                            slots.Count
                        )
                );
                newBooking.IfLeft(
                    err =>
                        _logger.LogWarning(
                            "Failed to persist camp booking for contact {ContactId} on field {FieldId}: {Error}",
                            command.ContactId,
                            command.FieldId,
                            err.Message
                        )
                );

                return newBooking.Match<Either<Error, Booking>>(b => b, err => err);
            },
            err => err
        );
    }

    public async Task<Either<Error, Booking>> UpdateAsync(UpdateBookingCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            _logger.LogWarning(
                "Rejected update for booking {BookingId}: name must not be empty",
                command.Id
            );
            return new Error(ErrorType.Invalid, "Booking name must not be empty");
        }

        var result = await _bookingGateway.UpdateAsync(
            command.Id,
            command.Name,
            command.ContactId,
            command.OverwriteSlotNames
        );

        result.IfRight(
            b =>
                _logger.LogInformation(
                    "Updated booking {BookingId}: name={Name}, contact={ContactId}",
                    b.Id,
                    command.Name,
                    command.ContactId
                )
        );
        result.IfLeft(
            err =>
                _logger.LogWarning(
                    "Failed to update booking {BookingId}: {Error}",
                    command.Id,
                    err.Message
                )
        );

        return result;
    }

    public async Task<Either<Error, Unit>> CancelBookingAsync(Guid id, string? cancellationReason)
    {
        var result = await _bookingGateway.CancelAsync(id, cancellationReason);

        result.IfRight(
            _ =>
                _logger.LogInformation(
                    "Cancelled booking {BookingId}: {CancellationReason}",
                    id,
                    cancellationReason ?? "none"
                )
        );
        result.IfLeft(
            err =>
                _logger.LogWarning("Failed to cancel booking {BookingId}: {Error}", id, err.Message)
        );

        return result;
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

        var slotsResult = candidatesResult.Bind(
            candidates => SlotAllocation.AllocateHalfField(field, sessions, name, candidates)
        );

        slotsResult.IfLeft(
            err =>
                _logger.LogWarning(
                    "Half-field booking rejected on field {FieldId}: {Error}",
                    field.Id,
                    err.Message
                )
        );

        return slotsResult;
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

    // One session list per week: week 0 is the sessions as given, each later week shifts
    // them all by 7 days while the first session still starts on or before EndDate.
    private static IEnumerable<List<(DateTime From, DateTime To, decimal Price)>> GenerateCampWeeks(
        CreateCampBookingCommand command
    )
    {
        var firstFrom = command.Sessions.Min(s => s.From);

        for (var week = 0; week == 0 || firstFrom.AddDays(7 * week) <= command.EndDate; week++)
        {
            var offset = 7 * week;
            yield return command.Sessions
                .Select(
                    s =>
                        (
                            From: s.From.AddDays(offset),
                            To: s.From.AddDays(offset).AddMinutes(s.Duration),
                            s.Price
                        )
                )
                .ToList();
        }
    }
}
