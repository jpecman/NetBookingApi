using LanguageExt;

namespace BookingApi.Application.Services.LoginService;

public interface ILoginService
{
    Task<Either<LoginError, LoginResponseModel>> LoginAsync(LoginRequestModel request);
}
