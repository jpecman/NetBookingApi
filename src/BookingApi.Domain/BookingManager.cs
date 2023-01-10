using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using LanguageExt;
using static LanguageExt.Prelude;

namespace BookingApi.Domain;

public interface IBookingManager
{
    Task<Either<Error, Booking>> Create(Booking booking);
}

public class BookingManager : IBookingManager
{
    private readonly IBookingRepository _bookingRepository;

    public BookingManager(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<Either<Error, Booking>> Create(Booking booking)
    {
        var availableResponse = await IsAvailableAsync(booking);

        return await availableResponse.MatchAsync<Either<Error, Booking>>(
            async isAvailable =>
            {
                if (isAvailable)
                {
                    var newBooking = await _bookingRepository.Create(booking);

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

    private async Task<Either<Error, bool>> IsAvailableAsync(Booking booking)
    {
        var repoResponse = await _bookingRepository.Get(
            new BookingSearch(booking.Date, booking.From, booking.To, booking.Id)
        );

        return repoResponse.Match<Either<Error, bool>>(
            bookings =>
            {
                return bookings.Any(a => a.From <= booking.To && a.To >= booking.From);
            },
            error => error
        );
    }
}
