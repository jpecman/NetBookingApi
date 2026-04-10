using System.Collections.Immutable;

using BookingApi.Common.Messaging;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Search;

using LanguageExt;

namespace BookingApi.Domain.Gateways;

public interface ISlotGateway
{
    Task<Either<Error, ImmutableArray<Slot>>> GetAsync(SlotSearch search);
}
