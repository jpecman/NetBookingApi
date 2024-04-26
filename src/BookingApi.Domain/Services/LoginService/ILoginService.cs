using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LanguageExt;

namespace BookingApi.Domain.Services.LoginService
{
    public interface ILoginService
    {
        Task<Either<LoginError, LoginResponseModel>> LoginAsync(LoginRequestModel request);
    }
}
