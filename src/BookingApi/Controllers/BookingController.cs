
using BookingApi.Domain;
using BookingApi.Models.Bookings;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

public class BookingController : BaseApiController
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
        var createResponse = await _bookingManager.CreateAsync(booking.ToDomain());

        return HandleResponse(createResponse, x => x.ToResponse());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookingResponse>> GetAsync([FromRoute] Guid id)
    {
        var getResponse = await _bookingManager.GetAsync(id);

        return HandleResponse(getResponse, x => x.ToResponse());
    }
}
