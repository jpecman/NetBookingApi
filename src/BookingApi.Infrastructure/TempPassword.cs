using System.Security.Cryptography;

namespace BookingApi.Infrastructure;

/// <summary>
/// Generates temporary passwords that satisfy the Identity password policy
/// (length ≥ 10, upper, lower, digit, non-alphanumeric). Visually ambiguous
/// characters (0/O, 1/l/I) are excluded — these passwords get read out loud.
/// </summary>
public static class TempPassword
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnpqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Special = "!@#$%&*-_?";
    private const string All = Upper + Lower + Digits + Special;

    public static string Generate()
    {
        var chars = new char[16];

        chars[0] = Pick(Upper);
        chars[1] = Pick(Lower);
        chars[2] = Pick(Digits);
        chars[3] = Pick(Special);

        for (var i = 4; i < chars.Length; i++)
        {
            chars[i] = Pick(All);
        }

        // Fisher-Yates so the guaranteed character classes are not positional.
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    private static char Pick(string alphabet) =>
        alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
}
