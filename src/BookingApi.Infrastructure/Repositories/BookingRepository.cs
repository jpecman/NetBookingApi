using BookingApi.Domain;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
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

        public async Task<Either<Error, Booking>> Create(Booking booking)
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

        public async Task<Either<Error, ImmutableArray<Booking>>> Get(BookingSearch search)
        {
            try
            {
                var bookings = await _dbContext.Bookings.Where(x => x.From <= search.From && x.To >= search.To).ToListAsync();

                return Right(ImmutableArray.Create(bookings.Select(x => x.ToDomain()).ToArray()));
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<ImmutableArray<Booking>>(ex, ErrorType.General);
            }
        }
    }
}
