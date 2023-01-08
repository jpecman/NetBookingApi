using BookingApi.Domain.Entities;
using LanguageExt;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Domain.Repositories
{
    public interface IBookingRepository
    {
        Task<Either<Error, ImmutableArray<Booking>>> Get(BookingSearch search);

        Task<Either<Error, Booking>> Create(Booking booking);
    }

    public record BookingSearch(DateOnly Date, TimeOnly From, TimeOnly To, Guid ExceptId);
}
