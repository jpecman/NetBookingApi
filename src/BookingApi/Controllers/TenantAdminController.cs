using BookingApi.Domain.Gateways;
using BookingApi.Infrastructure;
using BookingApi.Models.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Controllers;

/// <summary>
/// Platform-level provisioning surface for the future admin app: tenants,
/// their API keys, and their memberships. SuperAdmin only.
/// </summary>
[Route("api/v1/admin/tenants")]
[Authorize(Roles = UserRoles.SuperAdmin)]
public class TenantAdminController(ITenantGateway tenantGateway, ILogger<TenantAdminController> logger)
    : BaseApiController(logger)
{
    [HttpGet]
    public async Task<ActionResult<TenantResponse[]>> GetAllAsync()
    {
        var result = await tenantGateway.GetAllAsync();
        return HandleResponse(result, tenants => tenants.Select(t => t.ToResponse()).ToArray());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TenantResponse>> GetAsync([FromRoute] Guid id)
    {
        var result = await tenantGateway.GetAsync(id);
        return HandleResponse(result, t => t.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<TenantResponse>> PostAsync(
        [FromBody] CreateTenantRequest request
    )
    {
        var result = await tenantGateway.CreateAsync(request.Name);
        return HandleResponse(result, t => t.ToResponse());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TenantResponse>> PutAsync(
        [FromRoute] Guid id,
        [FromBody] RenameTenantRequest request
    )
    {
        var result = await tenantGateway.RenameAsync(id, request.Name);
        return HandleResponse(result, t => t.ToResponse());
    }

    [HttpPost("{id}/apikey")]
    public async Task<ActionResult<TenantApiKeyResponse>> ResetApiKeyAsync([FromRoute] Guid id)
    {
        var result = await tenantGateway.ResetApiKeyAsync(id);
        return HandleResponse(result, key => new TenantApiKeyResponse(key));
    }

    [HttpGet("{id}/members")]
    public async Task<ActionResult<TenantMemberResponse[]>> GetMembersAsync([FromRoute] Guid id)
    {
        var result = await tenantGateway.GetMembersAsync(id);
        return HandleResponse(result, members => members.Select(m => m.ToResponse()).ToArray());
    }

    [HttpPost("{id}/members")]
    public async Task<ActionResult<TenantInviteResponse>> AddMemberAsync(
        [FromRoute] Guid id,
        [FromBody] AddTenantMemberRequest request
    )
    {
        var result = await tenantGateway.AddMemberAsync(id, request.Email, request.Role);
        return HandleResponse(result, invite => invite.ToResponse());
    }

    [HttpPost("{id}/members/{userId}/password")]
    public async Task<ActionResult<TenantMemberPasswordResponse>> ResetMemberPasswordAsync(
        [FromRoute] Guid id,
        [FromRoute] string userId
    )
    {
        var result = await tenantGateway.ResetMemberPasswordAsync(id, userId);
        return HandleResponse(result, password => new TenantMemberPasswordResponse(password));
    }

    [HttpPut("{id}/members/{userId}")]
    public async Task<ActionResult<TenantMemberResponse>> SetMemberRoleAsync(
        [FromRoute] Guid id,
        [FromRoute] string userId,
        [FromBody] SetTenantMemberRoleRequest request
    )
    {
        var result = await tenantGateway.SetMemberRoleAsync(id, userId, request.Role);
        return HandleResponse(result, m => m.ToResponse());
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<ActionResult> RemoveMemberAsync(
        [FromRoute] Guid id,
        [FromRoute] string userId
    )
    {
        var result = await tenantGateway.RemoveMemberAsync(id, userId);
        return result.Match<ActionResult>(_ => NoContent(), HandleError);
    }
}
