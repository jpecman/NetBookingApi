using System.Text.Json;

using BookingApi.Common.Messaging;
using BookingApi.Domain;
using BookingApi.Models;

using LanguageExt;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BookingApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public abstract class BaseApiController(ILogger logger) : ControllerBase
    {
        protected ILogger Logger { get; } = logger;

        protected ActionResult HandleError(Error error)
        {
            if (error.ErrorType == ErrorType.NotFound)
            {
                Logger.LogWarning("{Message}", error.Message);
                return NotFound();
            }

            if (error.ErrorType == ErrorType.Conflict)
            {
                Logger.LogWarning("{Message}", error.Message);
                return Conflict();
            }

            Logger.LogError("{Message}", error.Message);
            return new BadRequestObjectResult(JsonSerializer.Serialize(new ErrorResponse(new []{ error.Message}), new JsonSerializerOptions() { WriteIndented = true}));
        }

        protected ActionResult HandleResponse<Tin, Tout>(Either<Error, Tin> response, Func<Tin, Tout> toResponse)
        {
            return response.Match<ActionResult>(
                model => Ok(toResponse(model)),
                error => HandleError(error));
        }
    }
}
