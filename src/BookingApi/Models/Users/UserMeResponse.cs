namespace BookingApi.Models.Users
{
    public record UserMeResponse(string Id, string Email, IEnumerable<string> Roles);
}
