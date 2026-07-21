using LanguageExt;

namespace BookingApi.Application.Services.LoginService;

public interface ILoginService
{
    Task<Either<LoginError, LoginResponseModel>> LoginAsync(LoginRequestModel request);

    /// <summary>Authenticates with the current (possibly temporary) password,
    /// sets the new one, clears the must-change flag, and completes the login.</summary>
    Task<Either<LoginError, LoginResponseModel>> ChangePasswordAsync(
        ChangePasswordRequestModel request
    );
}
