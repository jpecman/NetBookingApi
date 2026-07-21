using Microsoft.AspNetCore.Authentication;

namespace BookingApi.Auth;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string Scheme = "ApiKey";
    public string HeaderName { get; set; } = "X-Api-Key";
}
