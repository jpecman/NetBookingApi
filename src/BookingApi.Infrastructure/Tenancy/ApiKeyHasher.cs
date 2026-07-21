using System.Security.Cryptography;
using System.Text;

namespace BookingApi.Infrastructure.Tenancy;

/// <summary>
/// API keys are stored as SHA-256 hashes so the database never holds the key
/// itself; the deterministic hash doubles as the lookup column.
/// </summary>
public static class ApiKeyHasher
{
    public static string Hash(string apiKey)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
    }
}
