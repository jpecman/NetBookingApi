
using BookingApi.Domain;
using BookingApi.Models;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

[ApiController]
// [ApiVersion("1.0")]
[Route("api/v1/[controller]")]
public class BookingController : ControllerBase
{
    private readonly ILogger<BookingController> _logger;
    private readonly IBookingManager _bookingManager;

    public BookingController(ILogger<BookingController> logger, IBookingManager bookingManager)
    {
        _logger = logger;
        _bookingManager = bookingManager;
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> PostAsync([FromBody] BookingRequest booking)
    {
        return HandleResponse(await _bookingManager.Create(booking.ToDomain()));
    }

    protected ActionResult HandleError(Error error)
    {
        if (error.ErrorType == ErrorType.NotFound)
        {
            return NotFound();
        }

        return BadRequest();
    }

    protected ActionResult HandleResponse<T>(Either<Error, T> response)
    {
        return response.Match<ActionResult>(
            model => Ok(model),
            error => HandleError(error));
    }
}
