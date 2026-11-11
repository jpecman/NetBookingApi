namespace BookingApi.Infrastructure.Services.LoginService;

public static class LoginClaims
{
    /// <summary>
    /// The user's Identity security stamp at login; a token whose stamp no
    /// longer matches the database is rejected (see TokenSessionValidator).
    /// </summary>
    public const string SecurityStamp = "security_stamp";
}
