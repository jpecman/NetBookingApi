namespace BookingApi.Domain.Search;

public record SlotSearch(DateTime From, DateTime To, bool IncludeCancelled = false);
