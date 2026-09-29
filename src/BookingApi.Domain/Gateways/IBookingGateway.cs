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

        /// <summary>Updates booking metadata. On rename, non-cancelled slots still carrying
        /// the old booking name follow it; if any slot has a custom name the update fails
        /// with Conflict unless <paramref name="overwriteSlotNames"/> is set.</summary>
        Task<Either<Error, Booking>> UpdateAsync(
            Guid id,
            string name,
            Guid contactId,
            bool overwriteSlotNames
        );

        Task<Either<Error, Unit>> CancelAsync(Guid bookingId, string? cancellationReason);
    }
}
