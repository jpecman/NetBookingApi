using BookingApi.Application.Services.LoginService;

namespace BookingApi.Models.Users
{
    public record UserLoginRequest(string Email, string Password);

    public static class UserLoginRequestExtensions
    {
        public static LoginRequestModel ToDomain(this UserLoginRequest request)
        {
            return new LoginRequestModel(request.Email, request.Password);
        }
    }
}
