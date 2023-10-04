using BookingApi.Common.Messaging;
using BookingApi.Domain.Services.LoginService;
using BookingApi.Models.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers
{
    public class UserController : BaseApiController
    {
        private readonly ILoginService _loginService;

        public UserController(ILoginService loginService)
        {
            _loginService = loginService;
        }

        [HttpPut("login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserLoginResponse>> LoginAsync([FromBody] UserLoginRequest request)
        {
            var result = await _loginService.LoginAsync(request.ToDomain());

            return result.Match<ActionResult>(
                success => Ok(new UserLoginResponse(success.Token)),
                error => HandleError(Error.Create(ErrorType.General))
            );
        }
    }
}
