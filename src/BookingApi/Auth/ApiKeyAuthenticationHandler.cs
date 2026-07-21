using System.Security.Claims;
using System.Text.Encodings.Web;

using BookingApi.Infrastructure;
using BookingApi.Infrastructure.Tenancy;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BookingApi.Auth;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    BookingApiDbContext dbContext
) : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var headerValue))
            return AuthenticateResult.NoResult();

        var providedHash = ApiKeyHasher.Hash(headerValue.ToString());
        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(t =>
            t.ApiKeyHash == providedHash
        );

        if (tenant is null)
            return AuthenticateResult.Fail("Invalid API key");

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "ApiKeyClient"),
            new Claim(TenantClaims.TenantId, tenant.Id.ToString()),
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}
