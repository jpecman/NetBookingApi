using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingApi.Common.Messaging
{
    public abstract class Response
    {
        public bool Result { get; protected set; }

        public ErrorType Error { get; set; }
    }

    public abstract class Response<T> : Response
    {
    }

    public class Success<T> : Response<T>
    {
        public T Data { get; set; }

        public Success()
        {
            Result = true;
        }
    }

    public class Failure<T> : Response<T>
    {
        public Failure()
        {
            Result = false;
        }
    }
}
