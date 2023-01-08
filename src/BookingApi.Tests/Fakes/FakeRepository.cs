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
        private readonly Dictionary<(DateOnly, TimeOnly, TimeOnly), Booking> _storage = new Dictionary<(DateOnly, TimeOnly, TimeOnly), Booking>();

        public async Task<Either<Error, Booking>> Create(Booking booking)
        {
            (DateOnly, TimeOnly, TimeOnly) key = (booking.Date, booking.From, booking.To);

            if (_storage.ContainsKey(key))
            {
                return Left(new Error(ErrorType.Invalid, "Duplicate"));
            }

            var bookingWithId = booking with {  Id = Guid.NewGuid() };

            _storage.Add(key, bookingWithId);

            return Right(bookingWithId);
        }

        public Task<Either<Error, ImmutableArray<Booking>>> Get(BookingSearch search)
        {
            throw new NotImplementedException();
        }
    }
}
