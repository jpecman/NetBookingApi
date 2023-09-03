using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using LanguageExt;
using System.Collections.Immutable;

namespace BookingApi.Domain.Repositories
{
    public interface IBookingRepository
    {
        Task<Either<Error, Booking>> GetAsync(Guid id);

        Task<Either<Error, ImmutableArray<Booking>>> GetAsync(BookingSearch search);

        Task<Either<Error, Guid>> CreateAsync(Booking booking);
    }

    public record BookingSearch(DateTime From, DateTime To, Guid ExceptId);
}
