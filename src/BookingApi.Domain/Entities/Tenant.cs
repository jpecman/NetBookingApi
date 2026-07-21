namespace BookingApi.Domain.Entities;

public record Tenant(Guid Id, string Name, bool HasApiKey);
