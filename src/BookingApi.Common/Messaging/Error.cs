using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Common.Messaging
{
    public record Error
    (
        ErrorType ErrorType,
        string Message // TODO: there should be no message, log it and return error type
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
}
