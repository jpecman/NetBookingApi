using System.Net.NetworkInformation;

namespace BookingApi.Domain;

public record Error
(
    ErrorType ErrorType,

    string Message
)
{
    public static Error Create(ErrorType errorType) => new Error(errorType, string.Empty);
}

public enum ErrorType
{
    General = 0,
    Invalid = 1,
    NotFound = 2
}