using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Infrastructure.Services.LoginService
{
    public  class LoginServiceOptions
    {
        public string SigningKey { get; set; } = null!;

        // Matches the auth cookie's lifetime set in AuthController.
        public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(8);
    }
}
