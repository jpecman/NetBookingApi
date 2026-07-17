using BookingApi.Domain.Gateways;
using BookingApi.Models.Fields;
using BookingApi.Models.Pitches;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

public class PitchController(IPitchGateway pitchGateway, ILogger<PitchController> logger)
    : BaseApiController(logger)
{
    [HttpPost]
    public async Task<ActionResult<PitchResponse>> PostAsync([FromBody] CreatePitchRequest request)
    {
        var result = await pitchGateway.CreateAsync(request.FieldId, request.Name);

        return HandleResponse(result, p => p.ToResponse());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PitchResponse>> PutAsync(
        Guid id,
        [FromBody] UpdatePitchRequest request
    )
    {
        var result = await pitchGateway.UpdateAsync(id, request.Name);

        return HandleResponse(result, p => p.ToResponse());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync(Guid id)
    {
        var result = await pitchGateway.DeleteAsync(id);

        return result.Match<ActionResult>(_ => NoContent(), HandleError);
    }
}
