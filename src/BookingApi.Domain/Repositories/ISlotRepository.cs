using System.Collections.Immutable;

using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Search;

using LanguageExt;

namespace BookingApi.Domain.Repositories;

public interface ISlotRepository
{
    Task<Either<Error, ImmutableArray<Slot>>> GetAsync(SlotSearch search);
}
