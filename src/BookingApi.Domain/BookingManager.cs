using BookingApi.Common.Messaging;
using BookingApi.Domain.Commands;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Domain.Search;

using LanguageExt;

using static LanguageExt.Prelude;

namespace BookingApi.Domain;

public interface IBookingManager
{
    Task<Either<Error, Booking>> CreateAsync(CreateSingleBookingCommand command);

    Task<Either<Error, Booking>> CreateRecurringAsync(CreateRecurringBookingCommand booking);

    Task<Either<Error, Booking>> GetAsync(Guid id);

    Task<Either<Error, Booking[]>> GetAsync(BookingSearch search);
}

public class BookingManager : IBookingManager
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IFieldRepository _fieldRepository;

    public BookingManager(
        IBookingRepository bookingRepository,
        IFieldRepository fieldRepository
    )
    {
        _bookingRepository = bookingRepository;
        _fieldRepository = fieldRepository;
    }

    public async Task<Either<Error, Booking>> CreateAsync(CreateSingleBookingCommand command)
    {
        var fieldId = command.FieldId;

        var allPitchesForField = await _fieldRepository.GetAsync(fieldId);


        var booking = new Booking(Guid.NewGuid(), string.Empty, null, null);


        var newBooking = await _bookingRepository.CreateAsync(booking);

        return newBooking.Match<Either<Error, Booking>>(b => b, err => err);

    }

    public async Task<Either<Error, Booking>> CreateRecurringAsync(CreateRecurringBookingCommand command)
    {
        var booking = new Booking(Guid.NewGuid(), string.Empty, null, null);

        var newBooking = await _bookingRepository.CreateAsync(booking);

        return newBooking.Match<Either<Error, Booking>>(b => b, err => err);
    }

    public Task<Either<Error, Booking>> GetAsync(Guid id)
    {
        return _bookingRepository.GetAsync(id);
    }

    public async Task<Either<Error, Booking[]>> GetAsync(BookingSearch search)
    {
        var response = await _bookingRepository.GetAsync(search);

        return await response.MatchAsync<Either<Error, Booking[]>>(
                        async bookings =>
                        {
                            return bookings.ToArray();
                        },
                        error => error
                        );
    }
}
