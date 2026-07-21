using BookingApi.Infrastructure;
using BookingApi.Infrastructure.DbEntities;
using BookingApi.Infrastructure.Tenancy;
using BookingApi.Tests.Fixtures;

using Microsoft.EntityFrameworkCore;

namespace BookingApi.Tests
{
    // Covers the lookup the ApiKeyAuthenticationHandler performs: a presented
    // key is hashed and resolved to exactly one tenant.
    [TestClass]
    public class TenantApiKeyTests
    {
        private static readonly Guid KeyedTenantId = new("B9C0D1E2-F3A4-5678-BC89-DE0123456789");

        private BookingApiDbContext _dbContext = null!;

        [TestInitialize]
        public async Task InitializeAsync()
        {
            var options = new DbContextOptionsBuilder()
                .UseNpgsql(PostgresAssemblyFixture.ConnectionString)
                .Options;
            _dbContext = new BookingApiDbContext(options, new DefaultCurrentTenant());

            if (!await _dbContext.Tenants.AnyAsync(t => t.Id == KeyedTenantId))
            {
                _dbContext.Tenants.Add(
                    new TenantDb
                    {
                        Id = KeyedTenantId,
                        Name = "Keyed Tenant",
                        ApiKeyHash = ApiKeyHasher.Hash("keyed-tenant-api-key"),
                    }
                );
                await _dbContext.SaveChangesAsync();
            }
        }

        [TestCleanup]
        public async Task CleanupAsync()
        {
            await _dbContext.DisposeAsync();
        }

        [TestMethod]
        public async Task PresentedKey_ResolvesToItsTenant()
        {
            var hash = ApiKeyHasher.Hash("keyed-tenant-api-key");
            var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t =>
                t.ApiKeyHash == hash
            );

            Assert.IsNotNull(tenant);
            Assert.AreEqual(KeyedTenantId, tenant.Id);
        }

        [TestMethod]
        public async Task UnknownKey_ResolvesToNoTenant()
        {
            var hash = ApiKeyHasher.Hash("some-key-nobody-issued");
            var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t =>
                t.ApiKeyHash == hash
            );

            Assert.IsNull(tenant);
        }
    }
}
