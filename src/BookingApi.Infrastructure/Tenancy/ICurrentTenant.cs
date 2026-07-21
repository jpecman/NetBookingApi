namespace BookingApi.Infrastructure.Tenancy;

public interface ICurrentTenant
{
    Guid TenantId { get; }
}
