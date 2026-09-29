using BookingApi.Application;
using BookingApi.Controllers;
using BookingApi.Domain.Gateways;
using BookingApi.Infrastructure;
using BookingApi.Infrastructure.DbEntities;
using BookingApi.Infrastructure.Gateways;
using BookingApi.Infrastructure.Tenancy;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingApi.Tests.Fixtures;

public abstract class BookingApiTestBase
{
    protected static readonly Guid FieldId = new("1E7AAE86-F1A9-4B39-85B1-778CACD5732E");
    protected static readonly Guid ContactId = new("BE20E86E-D5B3-4BD6-A5A9-1B705D1FC88C");
    protected static readonly Guid OtherContactId = new("C1D2E3F4-A5B6-7890-BC12-DE34FA567890");
    protected static readonly Guid PitchAId = new("A3C1D2E4-F5B6-7890-AB12-CD34EF567890");
    protected static readonly Guid PitchBId = new("B4D2E3F5-A6C7-8901-BC23-DE45FA678901");

    private ServiceProvider _provider = null!;

    protected TestCurrentTenant CurrentTenant { get; private set; } = null!;

    protected BookingController Controller => Resolve<BookingController>();

    protected T Resolve<T>()
        where T : notnull => _provider.GetRequiredService<T>();

    [TestInitialize]
    public async Task BaseInitializeAsync()
    {
        CurrentTenant = new TestCurrentTenant();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentTenant>(CurrentTenant);
        services.AddDbContext<BookingApiDbContext>(o =>
            o.UseNpgsql(PostgresAssemblyFixture.ConnectionString)
        );
        services.AddScoped<IBookingGateway, BookingGateway>();
        services.AddScoped<IFieldGateway, FieldGateway>();
        services.AddScoped<ISlotGateway, SlotGateway>();
        services.AddScoped<IBookingManager, BookingManager>();
        services.AddScoped<ISlotManager, SlotManager>();
        services.AddScoped<BookingController>();
        services.AddScoped<SlotController>();

        _provider = services.BuildServiceProvider();

        await ResetAndSeedAsync();
        await SeedAdditionalAsync();
    }

    [TestCleanup]
    public void BaseCleanup()
    {
        _provider.Dispose();
    }

    protected virtual Task SeedAdditionalAsync() => Task.CompletedTask;

    private async Task ResetAndSeedAsync()
    {
        var dbContext = Resolve<BookingApiDbContext>();

        await dbContext.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"Slots\", \"Bookings\" CASCADE;"
        );

        if (!await dbContext.Fields.AnyAsync())
        {
            dbContext.Fields.Add(
                new FieldDb
                {
                    Id = FieldId,
                    Name = "Field 1",
                    Pitches =
                    [
                        new PitchDb
                        {
                            Id = PitchAId,
                            Name = "Pitch A",
                            FieldId = FieldId,
                        },
                        new PitchDb
                        {
                            Id = PitchBId,
                            Name = "Pitch B",
                            FieldId = FieldId,
                        },
                    ],
                }
            );
        }

        if (!await dbContext.Contacts.AnyAsync())
        {
            dbContext.Contacts.Add(
                new ContactDb
                {
                    Id = ContactId,
                    FirstName = "Test",
                    LastName = "Contact",
                    Email = "test.contact@example.com",
                    Phone = "5550100",
                }
            );
            dbContext.Contacts.Add(
                new ContactDb
                {
                    Id = OtherContactId,
                    FirstName = "Other",
                    LastName = "Contact",
                    Email = "other.contact@example.com",
                    Phone = "5550101",
                }
            );
        }

        await dbContext.SaveChangesAsync();
    }
}
