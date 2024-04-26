namespace BookingApi.Domain.Search
{
    public record ContactSearch(string FirstName, string LastName, string Email, string Phone)
    {
        public static ContactSearch Empty()
        {
            return new ContactSearch(string.Empty, string.Empty, string.Empty, string.Empty);
        }
    }
}
