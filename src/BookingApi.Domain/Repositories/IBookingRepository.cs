using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using LanguageExt;
using System.Collections.Immutable;

namespace BookingApi.Domain.Repositories
{
    public interface IBookingRepository
    {
        Task<Either<Error, Booking>> Get(Guid id);

        Task<Either<Error, ImmutableArray<Booking>>> Get(BookingSearch search);

        Task<Either<Error, Booking>> Create(Booking booking);
    }

    public record BookingSearch(DateTime From, DateTime To, Guid ExceptId);
}
