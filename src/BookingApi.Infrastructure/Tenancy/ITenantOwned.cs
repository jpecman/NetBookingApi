namespace BookingApi.Infrastructure.Tenancy;

public interface ITenantOwned
{
    Guid TenantId { get; set; }
}
