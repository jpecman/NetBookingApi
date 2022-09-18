using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using LanguageExt;
using System.Reflection.Metadata.Ecma335;
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
        var repoResponse = await _bookingRepository.Create(booking);

        if (repoResponse.IsLeft)
        {
            return repoResponse.LeftValue();
        }

        return Right(repoResponse.RightValue());

        // return Left(new Error(ErrorType.General));
    }
}

public static class LanguageExtensions
{
    public static L LeftValue<L, R>(this Either<L, R> either)
    {
        return either.Match(
            r => throw new InvalidOperationException("Either is right."),
            l => l
        );
    }

    public static R RightValue<L, R>(this Either<L, R> either)
    {
        return either.Match(
            r => r,
            l => throw new InvalidOperationException("Either is left.")
        );
    }
}