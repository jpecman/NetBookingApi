namespace BookingApi.Domain.Entities;

public record TenantMember(string UserId, string UserName, string Email, string Role);
