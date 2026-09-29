using BookingApi.Application;
using BookingApi.Models.Bookings;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

[Route("api/v1/bookings")]
public class BookingController(ILogger<BookingController> logger, IBookingManager bookingManager)
    : BaseApiController(logger)
{
    private readonly IBookingManager _bookingManager = bookingManager;

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> PostAsync([FromBody] BookingRequest booking)
    {
        var createResponse = await _bookingManager.CreateAsync(booking.ToDomain());

        return HandleResponse(createResponse, x => x.ToResponse());
    }

    [HttpPost("camp")]
    public async Task<ActionResult<BookingResponse[]>> PostCampAsync(
        [FromBody] CampBookingRequest request
    )
    {
        var createResponse = await _bookingManager.CreateCampAsync(request.ToDomain());

        return HandleResponse(
            createResponse,
            bookings => bookings.Select(b => b.ToResponse()).ToArray()
        );
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BookingResponse>> PutAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateBookingRequest request
    )
    {
        var updateResponse = await _bookingManager.UpdateAsync(request.ToDomain(id));

        return HandleResponse(updateResponse, x => x.ToResponse());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync(
        [FromRoute] Guid id,
        [FromBody] CancelRequest? request = null
    )
    {
        var result = await _bookingManager.CancelBookingAsync(id, request?.CancellationReason);
        return result.Match<ActionResult>(_ => NoContent(), HandleError);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingResponse>> GetAsync([FromRoute] Guid id)
    {
        var getResponse = await _bookingManager.GetAsync(id);

        return HandleResponse(getResponse, x => x.ToResponse());
    }

    [HttpGet]
    public async Task<ActionResult<BookingResponse[]>> GetAsync(
        [FromQuery] SearchBookingRequest search
    )
    {
        var getResponse = await _bookingManager.GetAsync(search.ToDomain());

        return HandleResponse(
            getResponse,
            bookings => bookings.Select(b => b.ToResponse()).ToArray()
        );
    }
}
