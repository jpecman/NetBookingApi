using BookingApi.Domain.Gateways;
using BookingApi.Domain.Search;
using BookingApi.Models.Slots;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

public class SlotController(ISlotGateway slotGateway, ILogger<SlotController> logger) : BaseApiController(logger)
{
    [HttpGet]
    public async Task<ActionResult<SlotResponse[]>> GetAsync([FromQuery] DateTime from, DateTime to)
    {
        var result = await slotGateway.GetAsync(new SlotSearch(from, to));

        return HandleResponse(result, slots => slots.Select(s => s.ToResponse()).ToArray());
    }
}
