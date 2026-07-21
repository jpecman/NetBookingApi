using BookingApi.Common.Messaging;
using BookingApi.Domain;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Npgsql;

using static LanguageExt.Prelude;

namespace BookingApi.Infrastructure.Gateways
{
    public abstract class Gateway
    {
        protected async Task<Either<Error, Result>> Execute<Parameters, Result>(
            Func<Parameters, Task<Result>> func,
            Parameters prms
        )
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
            catch (DbUpdateException dbEx)
                when (dbEx.InnerException is PostgresException
                    {
                        SqlState: PostgresErrorCodes.ExclusionViolation
                            or PostgresErrorCodes.UniqueViolation
                    }
                )
            {
                return CreateErrorResponse<Result>(dbEx, ErrorType.Conflict);
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
