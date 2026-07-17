using BookingApi.Application;
using BookingApi.Models.BookingSummaries;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

[Route("api/v1/booking-summary")]
public class BookingSummaryController(
    ILogger<BookingSummaryController> logger,
    IBookingManager bookingManager
) : BaseApiController(logger)
{
    private readonly IBookingManager _bookingManager = bookingManager;

    [HttpGet]
    public async Task<ActionResult<ContactBookingSummaryResponse[]>> GetAsync(
        [FromQuery] ContactBookingSummaryRequest search
    )
    {
        var result = await _bookingManager.GetGroupedByContactAsync(search.ToDomain());

        return HandleResponse(result, summaries => summaries.Select(s => s.ToResponse()).ToArray());
    }
}
