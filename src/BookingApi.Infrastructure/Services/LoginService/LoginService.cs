using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using BookingApi.Domain.Services.LoginService;

using LanguageExt;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Services.LoginService
{
    public class LoginService : ILoginService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LoginServiceOptions _options;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoginService(
            IHttpContextAccessor httpContextAccessor,
            LoginServiceOptions options,
            SignInManager<ApplicationUser> signInManager,
            ILogger<LoginService> logger,
            UserManager<ApplicationUser> userManager
        )
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options;
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<Either<LoginError, LoginResponseModel>> LoginAsync(LoginRequestModel request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Left(LoginError.UserNotFound);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);
            if (result.Succeeded)
            {
                return Right(new LoginResponseModel(IssueToken()));
            }

            return Left(LoginError.UserLockedOut);
        }

        private string IssueToken()
        {
            ClaimsIdentity claims = ((ClaimsIdentity)_httpContextAccessor.HttpContext.User.Identity) ?? new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.Sid, "1"));

            var now = DateTimeOffset.Now.UtcDateTime;

            var tokenHandler = new JwtSecurityTokenHandler();
            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_options.SigningKey));
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Issuer = "Issuer",
                Audience = "Audience",
                NotBefore = now.Subtract(TimeSpan.FromHours(1)),
                Expires = now.Add(TimeSpan.FromDays(365)),
                SigningCredentials = signingCredentials
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var serializedToken = tokenHandler.WriteToken(token);

            return serializedToken;
        }
    }
}
