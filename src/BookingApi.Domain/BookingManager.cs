using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using LanguageExt;

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

    public Task<Either<Error, Booking>> Create(Booking booking)
    {
        return _bookingRepository.Create(booking);
    }

    //private async Task<Either<Error, bool>> IsAvailableAsync(Booking booking)
    //{
    //    var repoResponse = await _bookingRepository.Get(new BookingSearch(booking.From.Date, booking.To.Date.AddDays(1), booking.Id));
    //}
}