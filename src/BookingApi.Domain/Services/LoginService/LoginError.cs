using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Domain.Services.LoginService
{
    public enum LoginError
    {
        InvalidParameters,
        UserNotFound,
        UserLockedOut
    }
}
