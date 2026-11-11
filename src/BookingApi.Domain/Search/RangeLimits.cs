using BookingApi.Common.Messaging;

namespace BookingApi.Domain.Search;

// Caps on request sizes, so a single request can't loop or load without bound.
public static class RangeLimits
{
    public const int MaxOccurrences = 52;

    public const int MaxSessionsPerWeek = 14;

    // 366 days so a full calendar year, leap or not, fits in one search.
    public static readonly TimeSpan MaxSearchRange = TimeSpan.FromDays(366);

    public static Error? CheckSearch(DateTime from, DateTime to)
    {
        if (to < from)
            return new Error(ErrorType.Invalid, "'to' must not be before 'from'.");

        if (to - from > MaxSearchRange)
            return new Error(ErrorType.Invalid, "A search range may not exceed one year.");

        return null;
    }

    // Weekly occurrences from first until endDate (inclusive); 1 without an end date.
    public static int CountWeeklyOccurrences(DateTime first, DateTime? endDate) =>
        endDate is null || endDate < first ? 1 : (int)((endDate.Value - first).TotalDays / 7) + 1;
}
