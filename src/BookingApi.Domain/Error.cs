namespace BookingApi.Domain;

public class Error
{
    public Error(ErrorType errorType)
    {
        ErrorType = errorType;
    }

    public ErrorType ErrorType { get; }
}

public enum ErrorType
{
    General = 0,
    Invalid = 1,
    NotFound = 2
}