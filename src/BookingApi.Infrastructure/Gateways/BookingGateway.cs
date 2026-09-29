using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;
using BookingApi.Domain.Search;
using BookingApi.Infrastructure.DbEntities;

using LanguageExt;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Logging;

using System.Collections.Immutable;
using System.Diagnostics;

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

        public async Task<Either<Error, Booking>> UpdateAsync(
            Guid id,
            string name,
            Guid contactId,
            bool overwriteSlotNames
        )
        {
            try
            {
                var entity = await _dbContext.Bookings
                    .Include(b => b.Slots)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity is null)
                    return Left(new Error(ErrorType.NotFound, $"Booking {id} not found"));

                if (!await _dbContext.Contacts.AnyAsync(c => c.Id == contactId))
                    return Left(new Error(ErrorType.NotFound, $"Contact {contactId} not found"));

                // Slot names are seeded from the booking name but can be edited per slot.
                // On rename, slots still carrying the old name follow it; custom names are
                // only replaced when the caller explicitly asks for it.
                if (entity.Name != name)
                {
                    var activeSlots = entity.Slots
                        .Where(s => s.Status != SlotStatus.Cancelled)
                        .ToList();

                    if (!overwriteSlotNames && activeSlots.Any(s => s.Name != entity.Name))
                        return Left(
                            new Error(
                                ErrorType.Conflict,
                                "Some slots have custom names; resend with overwriteSlotNames to replace them"
                            )
                        );

                    foreach (var slot in activeSlots)
                    {
                        slot.Name = name;
                    }
                }

                entity.Name = name;
                entity.ContactId = contactId;

                await _dbContext.SaveChangesAsync();

                return Right(await LoadAsync(id));
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<Booking>(ex, ErrorType.General);
            }
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
                var stopwatch = Stopwatch.StartNew();
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

                _logger.LogInformation(
                    "Loaded {SlotCount} slots for calendar range {From}-{To} in {ElapsedMs}ms",
                    slots.Count,
                    search.From,
                    search.To,
                    stopwatch.ElapsedMilliseconds
                );

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
                var loadQuery = GetLoadQuery();

                var entity = await loadQuery.FirstAsync(x => x.Id == bId);

                return entity.ToDomain();
            };

            return await Execute(func, id);
        }

        private IQueryable<BookingDb> GetLoadQuery()
        {
            return _dbContext.Bookings
                .Include(b => b.Contact)
                .Include(b => b.Slots)
                .ThenInclude(s => s.Pitch);
        }
    }
}
