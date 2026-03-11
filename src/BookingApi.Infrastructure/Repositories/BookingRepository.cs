using BookingApi.Common.Messaging;
using BookingApi.Domain;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Domain.Search;
using BookingApi.Infrastructure.DbEntities;

using LanguageExt;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Repositories
{
    public class BookingRepository : Repository, IBookingRepository
    {
        private readonly BookingApiDbContext _dbContext;
        private readonly ILogger<BookingRepository> _logger;

        public BookingRepository(BookingApiDbContext dbContext, ILogger<BookingRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Either<Error, Booking>> CreateAsync(Booking booking)
        {
            Func<Booking, Task<Booking>> func = async b =>
            {
                var dbEntity = b.ToEntity();

                var entry = _dbContext.Bookings.Add(dbEntity);
                await _dbContext.SaveChangesAsync();

                return entry.Entity.ToDomain();
            };

            return await Execute(func, booking);
        }

        public async Task<Either<Error, ImmutableArray<Booking>>> GetAsync(BookingSearch search)
        {
            try
            {
                var slots = await _dbContext.Slots.Include(s => s.Booking).ThenInclude(b => b.Contact).Include(s => s.Pitch).
                    Where(s => s.Duration.LowerBound.ToUniversalTime() >= search.From.ToUniversalTime()
                            && s.Duration.UpperBound.ToUniversalTime() <= search.To.ToUniversalTime()).ToListAsync();

                var bookings = slots.Select(s => s.Booking).DistinctBy(b => b.Id);

                return Right(ImmutableArray.Create(bookings.Select(x => x.ToDomain()).ToArray()));
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<ImmutableArray<Booking>>(ex, ErrorType.General);
            }
        }

        public async Task<Either<Error, Booking>> GetAsync(Guid id)
        {
            Func<Guid, Task<Booking>> func = async bId =>
            {
                var entity = await _dbContext.Bookings.Include(b => b.Contact).Include(b => b.Slots).ThenInclude(s => s.Pitch).FirstAsync(x => x.Id == bId);

                return entity.ToDomain();
            };

            return await Execute(func, id);
        }
    }
}
