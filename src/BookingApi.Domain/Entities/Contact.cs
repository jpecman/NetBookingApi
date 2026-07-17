using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Domain.Entities
{
    public record Contact
    (
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        bool Show
    )
    {
        public static Contact EmptyWithId(Guid id) => new(id, string.Empty, string.Empty, string.Empty, string.Empty, false);
    }
}
