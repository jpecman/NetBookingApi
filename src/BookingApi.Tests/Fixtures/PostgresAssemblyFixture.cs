using BookingApi.Infrastructure;
using BookingApi.Infrastructure.Tenancy;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace BookingApi.Tests.Fixtures;

[TestClass]
public class PostgresAssemblyFixture
{
    private static PostgreSqlContainer? _container;

    public static string ConnectionString { get; private set; } = string.Empty;

    [AssemblyInitialize]
    public static async Task InitializeAsync(TestContext _)
    {
        ContainerRuntime.Configure();

        _container = new PostgreSqlBuilder("postgres:14.1-alpine")
            .WithDatabase("BookingDb")
            .WithUsername("bookaneer")
            .WithPassword("***REMOVED***")
            .Build();

        await _container.StartAsync();

        ConnectionString = _container.GetConnectionString();

        var options = new DbContextOptionsBuilder().UseNpgsql(ConnectionString).Options;
        await using var dbContext = new BookingApiDbContext(options, new DefaultCurrentTenant());
        await dbContext.Database.MigrateAsync();
    }

    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
