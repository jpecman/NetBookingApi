using BookingApi.Domain;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static LanguageExt.Prelude;

namespace BookingApi.Tests.Fakes
{
    internal class FakeRepository : IBookingRepository
    {
        private readonly Dictionary<(DateOnly, TimeOnly, TimeOnly), Booking> _storage =
            new Dictionary<(DateOnly, TimeOnly, TimeOnly), Booking>();

        public async Task<Either<Error, Booking>> Create(Booking booking)
        {
            (DateOnly, TimeOnly, TimeOnly) key = (booking.Date, booking.From, booking.To);

            if (_storage.ContainsKey(key))
            {
                return Left(new Error(ErrorType.Invalid, "Duplicate"));
            }

            var bookingWithId = booking with { Id = Guid.NewGuid() };

            _storage.Add(key, bookingWithId);

            return Right(bookingWithId);
        }

        public async Task<Either<Error, ImmutableArray<Booking>>> Get(BookingSearch search)
        {
            List<Booking> matches = new List<Booking>();

            foreach (var item in _storage)
            {
                if (
                    item.Key.Item1 == search.Date
                    && item.Key.Item2 > search.From
                    && item.Key.Item3 < search.To
                )
                {
                    matches.Add(item.Value);
                }
            }

            return Right(matches.ToImmutableArray());
        }
    }
}
