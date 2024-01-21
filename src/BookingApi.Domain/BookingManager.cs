using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Domain.Search;

using LanguageExt;

using static LanguageExt.Prelude;

namespace BookingApi.Domain;

public interface IBookingManager
{
    Task<Either<Error, Booking>> CreateAsync(Booking booking);

    Task<Either<Error, Booking>> CreateRecurringAsync(Booking booking);

    Task<Either<Error, Booking>> GetAsync(Guid id);

    Task<Either<Error, Booking[]>> GetAsync(BookingSearch search);
}

public class BookingManager : IBookingManager
{
    private readonly IBookingRepository _bookingRepository;

    public BookingManager(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<Either<Error, Booking>> CreateAsync(Booking booking)
    {
        var availableResponse = await IsAvailableAsync(booking);

        return await availableResponse.MatchAsync<Either<Error, Booking>>(
            async isAvailable =>
            {
                if (isAvailable)
                {
                    var newBooking = await _bookingRepository.CreateAsync(booking);

                    return newBooking.Match<Either<Error, Booking>>(b => b, err => err);
                }
                else
                {
                    return Left(new Error(ErrorType.Invalid, "Already exists."));
                }
            },
            error => error
        );
    }

    public async Task<Either<Error, Booking>> CreateRecurringAsync(Booking booking)
    {
        var availableResponse = await IsAvailableAsync(booking);

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

    private async Task<Either<Error, bool>> IsAvailableAsync(Booking booking)
    {
        var repoResponse = await _bookingRepository.GetAsync(new BookingSearch(booking.From, booking.To, booking.Id));

        return repoResponse.Match<Either<Error, bool>>(
            bookings =>
            {
                return !bookings.Any(a => a.From <= booking.To && a.To >= booking.From);
            },
            error => error
        );
    }
}
