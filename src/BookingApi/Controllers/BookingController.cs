
using BookingApi.Application;
using BookingApi.Models.Bookings;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

public class BookingController(ILogger<BookingController> logger, IBookingManager bookingManager) : BaseApiController(logger)
{
    private readonly IBookingManager _bookingManager = bookingManager;

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> PostAsync([FromBody] BookingRequest booking)
    {
        var createResponse = await _bookingManager.CreateAsync(booking.ToDomain());

        return HandleResponse(createResponse, x => x.ToResponse());
    }

    [HttpPost("camp")]
    public async Task<ActionResult<BookingResponse>> PostCampAsync([FromBody] CampBookingRequest request)
    {
        var createResponse = await _bookingManager.CreateCampAsync(request.ToDomain());

        return HandleResponse(createResponse, x => x.ToResponse());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingResponse>> GetAsync([FromRoute] Guid id)
    {
        var getResponse = await _bookingManager.GetAsync(id);

        return HandleResponse(getResponse, x => x.ToResponse());
    }

    [HttpGet]
    public async Task<ActionResult<BookingResponse[]>> GetAsync([FromQuery] SearchBookingRequest search)
    {
        var getResponse = await _bookingManager.GetAsync(search.ToDomain());

        return HandleResponse(getResponse, bookings => bookings.Select(b => b.ToResponse()).ToArray());
    }
}
