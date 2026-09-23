using BookingApi.Application;
using BookingApi.Models.BookingSummaries;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

[Route("api/v1/reports")]
public class ReportController(
    ILogger<ReportController> logger,
    IBookingManager bookingManager
) : BaseApiController(logger)
{
    private readonly IBookingManager _bookingManager = bookingManager;

    [HttpGet("contact-bookings")]
    public async Task<ActionResult<ContactBookingSummaryResponse[]>> GetContactBookingsAsync(
        [FromQuery] ContactBookingSummaryRequest search
    )
    {
        var result = await _bookingManager.GetGroupedByContactAsync(search.ToDomain());

        return HandleResponse(result, summaries => summaries.Select(s => s.ToResponse()).ToArray());
    }
}
