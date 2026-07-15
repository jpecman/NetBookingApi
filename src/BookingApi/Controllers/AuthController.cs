using System.Security.Claims;

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
        [HttpGet("me")]
        public ActionResult<UserMeResponse> Me()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);

            if (id is null || email is null)
                return Unauthorized();

            return Ok(new UserMeResponse(id, email, roles));
        }

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

        [HttpPut("logout")]
        public ActionResult Logout()
        {
            HttpContext.Response.Cookies.Delete("token", new CookieOptions { Path = "/" });

            return Ok();
        }
    }
}
