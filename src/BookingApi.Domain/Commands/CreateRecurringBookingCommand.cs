using System;
using System.Collections.Generic;
using System.Text;

namespace BookingApi.Domain.Commands
{
    public record CreateRecurringBookingCommand
    (
        string Name,
        DateTime From,
        int DurationInMinutes,
        decimal Price,
        bool IsWholeField,
        Guid ContactId,
        Guid FieldId,
        DateTime StartDate,
        DateTime EndDate
    );
}
