using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Search;

using LanguageExt;
using System.Collections.Immutable;

namespace BookingApi.Domain.Gateways
{
    public interface IBookingGateway
    {
        Task<Either<Error, Booking>> GetAsync(Guid id);

        Task<Either<Error, ImmutableArray<Booking>>> GetAsync(BookingSearch search);

        Task<Either<Error, Booking>> CreateAsync(Booking booking);
    }
}
