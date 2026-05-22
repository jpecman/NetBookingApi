using BookingApi.Common.Messaging;
using BookingApi.Application.Services.LoginService;
using BookingApi.Models.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers
{
    public class AuthController(ILoginService loginService, ILogger<AuthController> logger)
        : BaseApiController(logger)
    {
        [HttpPut("login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserLoginResponse>> LoginAsync(
            [FromBody] UserLoginRequest request
        )
        {
            var result = await loginService.LoginAsync(request.ToDomain());

            return result.Match<ActionResult>(
                success =>
                {
                    HttpContext.Response.Cookies.Append(
                        "token",
                        success.Token,
                        new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = false,
                            SameSite = SameSiteMode.Lax,
                            Expires = DateTimeOffset.UtcNow.AddHours(8),
                            Path = "/"
                        }
                    );
                    return Ok(
                        new UserLoginResponse(success.Token, success.Email, success.UserName)
                    );
                },
                error => HandleError(Error.Create(ErrorType.General))
            );
        }
    }
}
