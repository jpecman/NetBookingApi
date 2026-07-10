namespace BookingApi.Domain.Search;

public record SlotSearch(DateTime From, DateTime To, Guid FieldId, bool IncludeCancelled = false);
