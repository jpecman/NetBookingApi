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
    Task<Either<Error, Booking>> CreateAsync(CreateBookingCommand command);

    Task<Either<Error, Booking>> CreateRecurringAsync(Booking booking);

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

    public async Task<Either<Error, Booking>> CreateAsync(CreateBookingCommand command)
    {
        var fieldId = command.FieldId;

        var allPitchesForField = await _fieldRepository.GetAsync(fieldId);

        // var availableResponse = await IsAvailableAsync(booking);
        var booking = new Booking(Guid.NewGuid(), string.Empty, null, null);


        var newBooking = await _bookingRepository.CreateAsync(booking);

        return newBooking.Match<Either<Error, Booking>>(b => b, err => err);

    }

    public async Task<Either<Error, Booking>> CreateRecurringAsync(Booking booking)
    {
        // var availableResponse = await IsAvailableAsync(booking);

        return await availableResponse.MatchAsync<Either<Error, Booking>>(
            async isAvailable => new Booking(Guid.Empty, string.Empty, null, null),
            error => error
            );
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

    // private async Task<Either<Error, bool>> IsAvailableAsync(Booking booking)
    // {
    //     var repoResponse = await _bookingRepository.GetAsync(new BookingSearch(booking.From, booking.To, booking.Id));
    //
    //     return repoResponse.Match<Either<Error, bool>>(
    //         bookings =>
    //         {
    //             return !bookings.Any(a => a.From <= booking.To && a.To >= booking.From);
    //         },
    //         error => error
    //     );
    // }
}
