using System.Security.Claims;

using BookingApi.Common.Messaging;
using BookingApi.Application.Services.LoginService;
using BookingApi.Models.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers
{
    [Route("api/v1/auth")]
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

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserLoginResponse>> LoginAsync(
            [FromBody] UserLoginRequest request
        )
        {
            var result = await loginService.LoginAsync(request.ToDomain());

            return result.Match<ActionResult>(SignIn, HandleLoginError);
        }

        [HttpPost("password")]
        [AllowAnonymous]
        public async Task<ActionResult<UserLoginResponse>> ChangePasswordAsync(
            [FromBody] UserChangePasswordRequest request
        )
        {
            var result = await loginService.ChangePasswordAsync(request.ToDomain());

            return result.Match<ActionResult>(SignIn, HandleLoginError);
        }

        private ActionResult SignIn(LoginResponseModel success)
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
            return Ok(new UserLoginResponse(success.Token, success.Email, success.UserName));
        }

        private ActionResult HandleLoginError(LoginError error)
        {
            // 403 is the SPA's signal to switch to the set-new-password step;
            // login produces no other 403s. Everything else stays a generic 400
            // so responses don't reveal whether the account exists.
            if (error == LoginError.PasswordChangeRequired)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            return HandleError(Error.Create(ErrorType.General));
        }

        [HttpPost("logout")]
        public ActionResult Logout()
        {
            HttpContext.Response.Cookies.Delete("token", new CookieOptions { Path = "/" });

            return Ok();
        }
    }
}
