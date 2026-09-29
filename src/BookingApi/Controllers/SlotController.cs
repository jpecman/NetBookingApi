using Microsoft.AspNetCore.Authorization;

using BookingApi.Application;
using BookingApi.Domain.Gateways;
using BookingApi.Domain.Search;
using BookingApi.Models.Bookings;
using BookingApi.Models.Slots;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

[Route("api/v1/slots")]
public class SlotController(
    ISlotGateway slotGateway,
    ISlotManager slotManager,
    ILogger<SlotController> logger
) : BaseApiController(logger)
{
    [HttpGet]
    [Authorize(Policy = "ApiKeyOrJwt")]
    public async Task<ActionResult<SlotResponse[]>> GetAsync(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] Guid fieldId,
        [FromQuery] bool includeCancelled = false
    )
    {
        var result = await slotGateway.GetAsync(
            new SlotSearch(from, to, fieldId, includeCancelled)
        );

        return HandleResponse(result, slots => slots.Select(s => s.ToResponse()).ToArray());
    }

    // All-or-nothing: either every slot is updated, or nothing is written.
    [HttpPatch]
    public async Task<ActionResult<SlotResponse[]>> PatchAsync(
        [FromBody] UpdateSlotsRequest request
    )
    {
        var result = await slotManager.UpdateAsync(request.ToDomain());

        return HandleResponse(result, slots => slots.Select(s => s.ToResponse()).ToArray());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync(
        [FromRoute] Guid id,
        [FromBody] CancelRequest? request = null
    )
    {
        var result = await slotGateway.CancelAsync(id, request?.CancellationReason);
        return result.Match<ActionResult>(_ => NoContent(), HandleError);
    }
}
