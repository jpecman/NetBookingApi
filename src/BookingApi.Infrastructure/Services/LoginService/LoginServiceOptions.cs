using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;

namespace BookingApi.Infrastructure.Services.LoginService
{
    public  class LoginServiceOptions
    {
        public string SigningKey { get; set; } = null!;

        // Matches the auth cookie's lifetime set in AuthController.
        public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(8);

        public string Issuer { get; set; } = "BookingApi";

        public string Audience { get; set; } = "BookingApi";

        public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));

        // Used by the JwtBearer setup, so issuing and validating share one definition.
        public TokenValidationParameters CreateValidationParameters() =>
            new()
            {
                IssuerSigningKey = CreateSigningKey(),
                ValidateIssuer = true,
                ValidIssuer = Issuer,
                ValidateAudience = true,
                ValidAudience = Audience,
                ValidateLifetime = true,
            };
    }
}
