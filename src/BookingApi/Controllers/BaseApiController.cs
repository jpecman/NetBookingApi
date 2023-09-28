using BookingApi.Common.Messaging;
using BookingApi.Domain;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        protected ActionResult HandleError(Error error)
        {
            if (error.ErrorType == ErrorType.NotFound)
            {
                return NotFound();
            }

            return BadRequest();
        }

        protected ActionResult HandleResponse<Tin, Tout>(Either<Error, Tin> response, Func<Tin, Tout> toResponse)
        {
            return response.Match<ActionResult>(
                model => Ok(toResponse(model)),
                error => HandleError(error));
        }
    }
}
