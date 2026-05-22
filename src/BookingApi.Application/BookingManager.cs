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

    Task<Either<Error, Booking>> CreateRecurringAsync(CreateRecurringBookingCommand booking);

    Task<Either<Error, Booking>> GetAsync(Guid id);

    Task<Either<Error, Booking[]>> GetAsync(BookingSearch search);
}

public class BookingManager : IBookingManager
{
    private readonly IBookingGateway _bookingGateway;
    private readonly IFieldGateway _fieldGateway;

    public BookingManager(IBookingGateway bookingGateway, IFieldGateway fieldGateway)
    {
        _bookingGateway = bookingGateway;
        _fieldGateway = fieldGateway;
    }

    public async Task<Either<Error, Booking>> CreateAsync(CreateSingleBookingCommand command)
    {
        var fieldResult = await _fieldGateway.GetAsync(command.FieldId);

        return await fieldResult.MatchAsync<Either<Error, Booking>>(
            async field =>
            {
                var to = command.From.AddMinutes((int)command.Duration);
                var slots = field.Pitches
                    .Select(
                        pitch =>
                            new Slot(
                                Guid.NewGuid(),
                                command.Name,
                                command.From,
                                to,
                                command.Price,
                                pitch.Id
                            )
                    )
                    .ToImmutableList();

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
    }

    public async Task<Either<Error, Booking>> CreateRecurringAsync(
        CreateRecurringBookingCommand command
    )
    {
        var booking = new Booking(Guid.NewGuid(), string.Empty, null, null);

        var newBooking = await _bookingGateway.CreateAsync(booking);

        return newBooking.Match<Either<Error, Booking>>(b => b, err => err);
    }

    public Task<Either<Error, Booking>> GetAsync(Guid id)
    {
        return _bookingGateway.GetAsync(id);
    }

    public async Task<Either<Error, Booking[]>> GetAsync(BookingSearch search)
    {
        var response = await _bookingGateway.GetAsync(search);

        return await response.MatchAsync<Either<Error, Booking[]>>(
            async bookings =>
            {
                return bookings.ToArray();
            },
            error => error
        );
    }
}
