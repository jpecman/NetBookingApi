
using BookingApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

[ApiController]
[Route("[controller]")]
public class BookingController : ControllerBase
{
    private readonly ILogger<BookingController> _logger;

    public BookingController(ILogger<BookingController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public Task<ActionResult<BookingResponse>> PostAsync([FromBody] BookingRequest booking)
    {
        return Task.FromResult(new ActionResult<BookingResponse>(new BookingResponse(Guid.Empty)));
    }
}
