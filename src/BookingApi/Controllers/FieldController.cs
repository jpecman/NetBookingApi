using BookingApi.Domain.Gateways;
using BookingApi.Models.Fields;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

public class FieldController(IFieldGateway fieldGateway, ILogger<FieldController> logger)
    : BaseApiController(logger)
{
    [HttpPost]
    public async Task<ActionResult<FieldResponse>> PostAsync([FromBody] CreateFieldRequest request)
    {
        var result = await fieldGateway.Create(request.ToDomain());

        return HandleResponse(result, f => f.ToResponse());
    }

    [HttpGet]
    public async Task<ActionResult<FieldResponse[]>> GetAsync()
    {
        var result = await fieldGateway.GetAllAsync();

        return HandleResponse(result, fields => fields.Select(f => f.ToResponse()).ToArray());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FieldResponse>> GetAsync([FromRoute] Guid id)
    {
        var result = await fieldGateway.GetAsync(id);

        return HandleResponse(result, f => f.ToResponse());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FieldResponse>> PutAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateFieldRequest request
    )
    {
        var result = await fieldGateway.Update(request.ToDomain(id));

        return HandleResponse(result, f => f.ToResponse());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync([FromRoute] Guid id)
    {
        var result = await fieldGateway.Delete(id);

        return result.Match<ActionResult>(_ => NoContent(), HandleError);
    }
}
