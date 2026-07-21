using BookingApi.Application.Services.LoginService;

namespace BookingApi.Models.Users
{
    public record UserLoginRequest(string Email, string Password);

    public record UserChangePasswordRequest(
        string Email,
        string CurrentPassword,
        string NewPassword
    );

    public static class UserLoginRequestExtensions
    {
        public static LoginRequestModel ToDomain(this UserLoginRequest request)
        {
            return new LoginRequestModel(request.Email, request.Password);
        }

        public static ChangePasswordRequestModel ToDomain(this UserChangePasswordRequest request)
        {
            return new ChangePasswordRequestModel(
                request.Email,
                request.CurrentPassword,
                request.NewPassword
            );
        }
    }
}
