using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using BookingApi.Infrastructure.Services.LoginService;

using Microsoft.IdentityModel.Tokens;

namespace BookingApi.Tests
{
    [TestClass]
    public class TokenValidationParametersTests
    {
        private static readonly LoginServiceOptions Options = new()
        {
            SigningKey = "integration-test-signing-key-0123456789abcdef",
        };

        [TestMethod]
        public void Token_WithMatchingIssuerAndAudience_IsAccepted()
        {
            var token = CreateToken(Options.Issuer, Options.Audience);

            Validate(token);
        }

        [TestMethod]
        public void Token_WithOtherIssuer_IsRejected()
        {
            var token = CreateToken("Issuer", Options.Audience);

            Assert.ThrowsExactly<SecurityTokenInvalidIssuerException>(() => Validate(token));
        }

        [TestMethod]
        public void Token_WithOtherAudience_IsRejected()
        {
            var token = CreateToken(Options.Issuer, "Audience");

            Assert.ThrowsExactly<SecurityTokenInvalidAudienceException>(() => Validate(token));
        }

        private static void Validate(string token) =>
            new JwtSecurityTokenHandler().ValidateToken(
                token,
                Options.CreateValidationParameters(),
                out _
            );

        private static string CreateToken(string issuer, string audience)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(
                new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user")]),
                    Issuer = issuer,
                    Audience = audience,
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(
                        Options.CreateSigningKey(),
                        SecurityAlgorithms.HmacSha256Signature
                    ),
                }
            );

            return handler.WriteToken(token);
        }
    }
}
