using BookingApi.Domain.Gateways;
using BookingApi.Infrastructure;
using BookingApi.Models.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

[Route("api/v1/admin/users")]
[Authorize(Roles = UserRoles.Administrator)]
public class AdminUserController(IUserGateway userGateway, ILogger<AdminUserController> logger)
    : BaseApiController(logger)
{
    [HttpGet]
    public async Task<ActionResult<UserResponse[]>> GetAllAsync()
    {
        var result = await userGateway.GetAllAsync();
        return HandleResponse(result, users => users.Select(u => u.ToResponse()).ToArray());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetAsync([FromRoute] string id)
    {
        var result = await userGateway.GetAsync(id);
        return HandleResponse(result, u => u.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> PostAsync([FromBody] CreateUserRequest request)
    {
        var result = await userGateway.CreateAsync(
            request.UserName,
            request.Email,
            request.Password
        );
        return HandleResponse(result, u => u.ToResponse());
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync([FromRoute] string id)
    {
        var result = await userGateway.DeleteAsync(id);
        return result.Match<ActionResult>(_ => NoContent(), HandleError);
    }
}
