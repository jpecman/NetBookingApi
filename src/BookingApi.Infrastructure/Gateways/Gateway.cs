using BookingApi.Common.Messaging;
using BookingApi.Domain;
using LanguageExt;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Gateways
{
    public abstract class Gateway
    {
        protected async Task<Either<Error, Result>> Execute<Parameters, Result>(Func<Parameters, Task<Result>> func, Parameters prms)
        {
            try
            {
                var result = await func(prms);

                return Right(result);
            }
            catch (InvalidOperationException invEx)
            {
                return CreateErrorResponse<Result>(invEx, ErrorType.NotFound);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse<Result>(ex, ErrorType.General);
            }
        }

        protected Either<Error, T> CreateErrorResponse<T>(Exception ex, ErrorType errorType)
        {
            return Left(new Error(errorType, ex.Message));
        }
    }
}
