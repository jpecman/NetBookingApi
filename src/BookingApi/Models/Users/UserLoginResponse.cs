namespace BookingApi.Models.Users
{
    public record UserLoginResponse(string Token, string Email, string UserName);
}
