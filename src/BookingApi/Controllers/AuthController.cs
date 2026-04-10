using BookingApi.Common.Messaging;
using BookingApi.Domain.Services.LoginService;
using BookingApi.Models.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers
{
    public class AuthController(ILoginService loginService, ILogger<AuthController> logger) : BaseApiController(logger)
    {
        [HttpPut("login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserLoginResponse>> LoginAsync([FromBody] UserLoginRequest request)
        {
            var result = await loginService.LoginAsync(request.ToDomain());

            return result.Match<ActionResult>(
                success => Ok(new UserLoginResponse(success.Token)),
                error => HandleError(Error.Create(ErrorType.General))
            );
        }
    }
}
