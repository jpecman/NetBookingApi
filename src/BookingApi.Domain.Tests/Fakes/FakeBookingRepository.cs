using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Domain.Search;

using LanguageExt;

using System.Collections.Immutable;

using static LanguageExt.Prelude;

namespace BookingApi.Domain.Tests.Fakes
{
    internal class FakeBookingRepository : IBookingRepository
    {
        private readonly Dictionary<(DateTime From, DateTime To), Booking> _storage =
            new Dictionary<(DateTime, DateTime), Booking>();

        public async Task<Either<Error, Booking>> CreateAsync(Booking booking)
        {
            (DateTime, DateTime) key = (booking.From, booking.To);

            if (_storage.ContainsKey(key))
            {
                return Left(new Error(ErrorType.Invalid, "Duplicate"));
            }

            var bookingWithId = booking with { Id = Guid.NewGuid() };

            _storage.Add(key, bookingWithId);

            return Right(bookingWithId);
        }

        public async Task<Either<Error, ImmutableArray<Booking>>> GetAsync(BookingSearch search)
        {
            List<Booking> matches = new List<Booking>();

            foreach (var item in _storage)
            {
                if (
                    // item.Key.Date == search.Date
                    item.Key.From > search.From
                    && item.Key.To < search.To
                )
                {
                    matches.Add(item.Value);
                }
            }

            return Right(matches.ToImmutableArray());
        }

        public async Task<Either<Error, Booking>> GetAsync(Guid id)
        {
            var booking = _storage.Where(x => x.Value.Id == id).FirstOrDefault();

            if (booking.Value == null)
            {
                return Left(new Error(ErrorType.NotFound, "Not found."));
            }

            return Right(booking.Value);
        }
    }
}
