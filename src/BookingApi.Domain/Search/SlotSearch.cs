namespace BookingApi.Domain.Search;

public record SlotSearch(
    DateTime From,
    DateTime To
)
{
    public SlotSearch ToDomain()
    {
        throw new NotImplementedException();
    }
}
