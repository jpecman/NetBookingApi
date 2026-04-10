using BookingApi.Domain.Gateways;
using BookingApi.Infrastructure;
using BookingApi.Models.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

[Route("api/v1/admin/roles")]
[Authorize(Roles = UserRoles.Administrator)]
public class AdminRoleController(IRoleGateway roleGateway, ILogger<AdminRoleController> logger) : BaseApiController(logger)
{
    [HttpGet]
    public async Task<ActionResult<RoleResponse[]>> GetAllAsync()
    {
        var result = await roleGateway.GetAllAsync();
        return HandleResponse(result, roles => roles.Select(r => r.ToResponse()).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<RoleResponse>> PostAsync([FromBody] CreateRoleRequest request)
    {
        var result = await roleGateway.CreateAsync(request.Name);
        return HandleResponse(result, r => r.ToResponse());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync(string id)
    {
        var result = await roleGateway.DeleteAsync(id);
        return result.Match<ActionResult>(_ => NoContent(), HandleError);
    }
}
