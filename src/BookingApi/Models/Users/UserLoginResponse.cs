namespace BookingApi.Models.Users
{
    // No token here: it travels only in the HttpOnly cookie, out of reach of page scripts.
    public record UserLoginResponse(string Email, string UserName);
}
