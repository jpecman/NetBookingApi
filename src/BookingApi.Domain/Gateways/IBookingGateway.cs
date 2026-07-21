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

        /// <summary>Updates booking metadata; the name is propagated to the
        /// booking's slots (slot names mirror the booking name).</summary>
        Task<Either<Error, Booking>> UpdateAsync(Guid id, string name, Guid contactId);

        /// <summary>Moves/reprices one session: every non-cancelled slot of the
        /// booking matching the current range gets the new range and price.</summary>
        Task<Either<Error, Booking>> UpdateSessionAsync(
            Guid bookingId,
            DateTime currentFrom,
            DateTime currentTo,
            DateTime newFrom,
            DateTime newTo,
            decimal price
        );

        Task<Either<Error, Unit>> CancelAsync(Guid bookingId, string? cancellationReason);
    }
}
