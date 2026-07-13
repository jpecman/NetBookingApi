namespace BookingApi.Domain.Search
{
    public record BookingSearch(
        DateTime From,
        DateTime To,
        Guid ExceptId,
        bool IncludeCancelled = false,
        Guid? ContactId = null
    );
}
