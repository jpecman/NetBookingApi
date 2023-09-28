namespace BookingApi.Domain.Search
{
    public record BookingSearch(DateTime From, DateTime To, Guid ExceptId);
}
