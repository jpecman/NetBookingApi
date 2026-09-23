using BookingApi.Domain.Gateways;
using BookingApi.Models.Contacts;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

[Route("api/v1/contacts")]
public class ContactController(IContactGateway contactGateway, ILogger<ContactController> logger)
    : BaseApiController(logger)
{
    [HttpPost]
    public async Task<ActionResult<ContactResponse>> PostAsync(
        [FromBody] CreateContactRequest request
    )
    {
        var result = await contactGateway.CreateAsync(request.ToDomain());

        return HandleResponse(result, c => c.ToResponse());
    }

    [HttpGet]
    public async Task<ActionResult<ContactResponse[]>> GetAsync()
    {
        var result = await contactGateway.GetAllAsync();

        return HandleResponse(result, contacts => contacts.Select(c => c.ToResponse()).ToArray());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContactResponse>> GetAsync([FromRoute] Guid id)
    {
        var result = await contactGateway.GetAsync(id);

        return HandleResponse(result, c => c.ToResponse());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ContactResponse>> PutAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateContactRequest request
    )
    {
        var result = await contactGateway.UpdateAsync(request.ToDomain(id));

        return HandleResponse(result, c => c.ToResponse());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync([FromRoute] Guid id)
    {
        var result = await contactGateway.DeleteAsync(id);

        return result.Match<ActionResult>(_ => NoContent(), HandleError);
    }
}
