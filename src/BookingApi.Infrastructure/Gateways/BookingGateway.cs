using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;
using BookingApi.Domain.Search;
using BookingApi.Infrastructure.DbEntities;

using LanguageExt;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NpgsqlTypes;

using System.Collections.Immutable;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Gateways
{
    public class BookingGateway : Gateway, IBookingGateway
    {
        private readonly BookingApiDbContext _dbContext;
        private readonly ILogger<BookingGateway> _logger;

        public BookingGateway(BookingApiDbContext dbContext, ILogger<BookingGateway> logger)
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

                var saved = await _dbContext.Bookings
                    .Include(b => b.Contact)
                    .Include(b => b.Slots)
                    .ThenInclude(s => s.Pitch)
                    .FirstAsync(b => b.Id == entry.Entity.Id);

                return saved.ToDomain();
            };

            return await Execute(func, booking);
        }

        public async Task<Either<Error, Booking>> UpdateAsync(Guid id, string name, Guid contactId)
        {
            Func<Guid, Task<Booking>> func = async bId =>
            {
                var entity = await _dbContext.Bookings
                    .Include(b => b.Slots)
                    .FirstAsync(x => x.Id == bId);

                if (!await _dbContext.Contacts.AnyAsync(c => c.Id == contactId))
                    throw new InvalidOperationException($"Contact {contactId} not found");

                entity.Name = name;
                entity.ContactId = contactId;
                // Slot names mirror the booking name (set at creation) — keep them in sync.
                foreach (var slot in entity.Slots)
                {
                    slot.Name = name;
                }

                await _dbContext.SaveChangesAsync();

                return await LoadAsync(bId);
            };

            return await Execute(func, id);
        }

        public async Task<Either<Error, Booking>> UpdateSessionAsync(
            Guid bookingId,
            DateTime currentFrom,
            DateTime currentTo,
            DateTime newFrom,
            DateTime newTo,
            decimal price
        )
        {
            Func<Guid, Task<Booking>> func = async bId =>
            {
                var entity = await _dbContext.Bookings
                    .Include(b => b.Slots)
                    .FirstAsync(x => x.Id == bId);

                var utcFrom = currentFrom.ToUniversalTime();
                var utcTo = currentTo.ToUniversalTime();

                var sessionSlots = entity.Slots
                    .Where(
                        s =>
                            s.Status != SlotStatus.Cancelled
                            && s.Duration.LowerBound == utcFrom
                            && s.Duration.UpperBound == utcTo
                    )
                    .ToList();

                if (sessionSlots.Count == 0)
                    throw new InvalidOperationException(
                        $"Booking {bId} has no session from {currentFrom:g} to {currentTo:g}"
                    );

                foreach (var slot in sessionSlots)
                {
                    slot.Duration = new NpgsqlRange<DateTime>(
                        newFrom.ToUniversalTime(),
                        lowerBoundIsInclusive: true,
                        newTo.ToUniversalTime(),
                        upperBoundIsInclusive: false
                    );
                    slot.Price = price;
                }

                // Overlaps with other bookings surface here as an exclusion
                // violation, which Execute maps to Conflict.
                await _dbContext.SaveChangesAsync();

                return await LoadAsync(bId);
            };

            return await Execute(func, bookingId);
        }

        private async Task<Booking> LoadAsync(Guid id)
        {
            var saved = await _dbContext.Bookings
                .Include(b => b.Contact)
                .Include(b => b.Slots)
                .ThenInclude(s => s.Pitch)
                .FirstAsync(b => b.Id == id);

            return saved.ToDomain();
        }

        public async Task<Either<Error, ImmutableArray<Booking>>> GetAsync(BookingSearch search)
        {
            try
            {
                var slots = await _dbContext.Slots
                    .Include(s => s.Booking)
                    .ThenInclude(b => b.Contact)
                    .Include(s => s.Pitch)
                    .Where(
                        s =>
                            s.Duration.LowerBound.ToUniversalTime() >= search.From.ToUniversalTime()
                            && s.Duration.UpperBound.ToUniversalTime()
                                <= search.To.ToUniversalTime()
                            && (search.IncludeCancelled || s.Status != SlotStatus.Cancelled)
                            && (search.ContactId == null || s.Booking.ContactId == search.ContactId)
                    )
                    .ToListAsync();

                var bookings = slots.Select(s => s.Booking).DistinctBy(b => b.Id);

                return Right(ImmutableArray.Create(bookings.Select(x => x.ToDomain()).ToArray()));
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<ImmutableArray<Booking>>(ex, ErrorType.General);
            }
        }

        public async Task<Either<Error, Unit>> CancelAsync(
            Guid bookingId,
            string? cancellationReason
        )
        {
            try
            {
                var slots = await _dbContext.Slots
                    .Where(s => s.BookingId == bookingId)
                    .ToListAsync();

                if (!slots.Any())
                    return Left(new Error(ErrorType.NotFound, $"Booking {bookingId} not found"));

                foreach (var slot in slots)
                {
                    slot.Status = SlotStatus.Cancelled;
                    slot.CancellationReason = cancellationReason;
                }

                await _dbContext.SaveChangesAsync();
                return Right(Unit.Default);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<Unit>(ex, ErrorType.General);
            }
        }

        public async Task<Either<Error, Booking>> GetAsync(Guid id)
        {
            Func<Guid, Task<Booking>> func = async bId =>
            {
                var entity = await _dbContext.Bookings
                    .Include(b => b.Contact)
                    .Include(b => b.Slots)
                    .ThenInclude(s => s.Pitch)
                    .FirstAsync(x => x.Id == bId);

                return entity.ToDomain();
            };

            return await Execute(func, id);
        }
    }
}
