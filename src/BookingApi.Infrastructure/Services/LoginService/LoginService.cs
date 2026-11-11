using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using BookingApi.Application.Services.LoginService;
using BookingApi.Infrastructure.DbEntities;
using BookingApi.Infrastructure.Tenancy;

using LanguageExt;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Services.LoginService
{
    public class LoginService : ILoginService
    {
        private readonly LoginServiceOptions _options;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly BookingApiDbContext _dbContext;

        public LoginService(
            LoginServiceOptions options,
            SignInManager<ApplicationUser> signInManager,
            ILogger<LoginService> logger,
            UserManager<ApplicationUser> userManager,
            BookingApiDbContext dbContext
        )
        {
            _options = options;
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
            _dbContext = dbContext;
        }

        public async Task<Either<LoginError, LoginResponseModel>> LoginAsync(
            LoginRequestModel request
        )
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Left(LoginError.UserNotFound);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                true
            );

            if (!result.Succeeded)
            {
                return Left(LoginError.UserLockedOut);
            }

            if (user.MustChangePassword)
            {
                return Left(LoginError.PasswordChangeRequired);
            }

            return await CompleteLoginAsync(user);
        }

        public async Task<Either<LoginError, LoginResponseModel>> ChangePasswordAsync(
            ChangePasswordRequestModel request
        )
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                return Left(LoginError.UserNotFound);
            }

            var check = await _signInManager.CheckPasswordSignInAsync(
                user,
                request.CurrentPassword,
                true
            );

            if (!check.Succeeded)
            {
                return Left(LoginError.UserLockedOut);
            }

            var change = await _userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword
            );

            if (!change.Succeeded)
            {
                _logger.LogWarning(
                    "Password change for user {UserId} rejected: {Errors}",
                    user.Id,
                    string.Join("; ", change.Errors.Select(e => e.Description))
                );
                return Left(LoginError.InvalidNewPassword);
            }

            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);

            return await CompleteLoginAsync(user);
        }

        private async Task<Either<LoginError, LoginResponseModel>> CompleteLoginAsync(
            ApplicationUser user
        )
        {
            // One membership per user for now; when the unique index on UserId
            // is dropped, this becomes a tenant selection step.
            var membership = await _dbContext.TenantMemberships.FirstOrDefaultAsync(m =>
                m.UserId == user.Id
            );

            if (membership == null)
            {
                _logger.LogWarning(
                    "User {UserId} has no tenant membership; refusing login",
                    user.Id
                );
                return Left(LoginError.NoTenantMembership);
            }

            // The token carries the stamp for revocation checks; a user created
            // outside UserManager could lack one.
            if (string.IsNullOrEmpty(user.SecurityStamp))
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }

            var isSuperAdmin = await _userManager.IsInRoleAsync(user, UserRoles.SuperAdmin);

            return Right(
                new LoginResponseModel(
                    IssueToken(user, membership, isSuperAdmin),
                    user.Email ?? string.Empty,
                    user.UserName ?? string.Empty
                )
            );
        }

        private string IssueToken(
            ApplicationUser user,
            TenantMembershipDb membership,
            bool isSuperAdmin
        )
        {
            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claims.AddClaim(new Claim(ClaimTypes.Email, user.Email ?? string.Empty));
            claims.AddClaim(new Claim(TenantClaims.TenantId, membership.TenantId.ToString()));
            claims.AddClaim(new Claim(ClaimTypes.Role, membership.Role));
            claims.AddClaim(new Claim(LoginClaims.SecurityStamp, user.SecurityStamp!));

            if (isSuperAdmin)
            {
                claims.AddClaim(new Claim(ClaimTypes.Role, UserRoles.SuperAdmin));
            }

            var now = DateTimeOffset.Now.UtcDateTime;

            var tokenHandler = new JwtSecurityTokenHandler();
            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_options.SigningKey));
            var signingCredentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256Signature
            );

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Issuer = "Issuer",
                Audience = "Audience",
                NotBefore = now.Subtract(TimeSpan.FromHours(1)),
                Expires = now.Add(_options.TokenLifetime),
                SigningCredentials = signingCredentials
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var serializedToken = tokenHandler.WriteToken(token);

            return serializedToken;
        }
    }
}
