using System.Collections.Immutable;

using BookingApi.Application.Commands;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Gateways;
using BookingApi.Infrastructure;
using BookingApi.Infrastructure.DbEntities;
using BookingApi.Models.Bookings;
using BookingApi.Tests.Fixtures;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Tests
{
    [TestClass]
    public class TenantIsolationTests : BookingApiTestBase
    {
        protected static readonly Guid TenantBId = new("D5E6F7A8-B9C0-1234-DE45-FA6789012345");
        protected static readonly Guid TenantBFieldId = new("E6F7A8B9-C0D1-2345-EF56-AB7890123456");
        protected static readonly Guid TenantBPitchId = new("F7A8B9C0-D1E2-3456-FA67-BC8901234567");
        protected static readonly Guid TenantBContactId = new("A8B9C0D1-E2F3-4567-AB78-CD9012345678");

        protected override async Task SeedAdditionalAsync()
        {
            var dbContext = Resolve<BookingApiDbContext>();

            if (!await dbContext.Tenants.AnyAsync(t => t.Id == TenantBId))
            {
                dbContext.Tenants.Add(new TenantDb { Id = TenantBId, Name = "Tenant B" });
            }

            if (
                !await dbContext
                    .Fields.IgnoreQueryFilters()
                    .AnyAsync(f => f.Id == TenantBFieldId)
            )
            {
                dbContext.Fields.Add(
                    new FieldDb
                    {
                        Id = TenantBFieldId,
                        TenantId = TenantBId,
                        Name = "Tenant B Field",
                        Pitches =
                        [
                            new PitchDb
                            {
                                Id = TenantBPitchId,
                                TenantId = TenantBId,
                                Name = "Tenant B Pitch",
                                FieldId = TenantBFieldId,
                            },
                        ],
                    }
                );
            }

            if (
                !await dbContext
                    .Contacts.IgnoreQueryFilters()
                    .AnyAsync(c => c.Id == TenantBContactId)
            )
            {
                // Same email as the default tenant's contact: seeding succeeds only
                // because the unique email index is scoped per tenant.
                dbContext.Contacts.Add(
                    new ContactDb
                    {
                        Id = TenantBContactId,
                        TenantId = TenantBId,
                        FirstName = "TenantB",
                        LastName = "Contact",
                        Email = "test.contact@example.com",
                        Phone = "5550200",
                    }
                );
            }

            await dbContext.SaveChangesAsync();
        }

        // Each tenant only sees its own fields.
        [TestMethod]
        public async Task GetAllFields_ReturnsOnlyActingTenantsFields()
        {
            var fieldGateway = Resolve<IFieldGateway>();

            var defaultTenantFields = (await fieldGateway.GetAllAsync()).RightAsEnumerable()
                .Single();

            Assert.IsTrue(defaultTenantFields.Any(f => f.Id == FieldId));
            Assert.IsFalse(defaultTenantFields.Any(f => f.Id == TenantBFieldId));

            CurrentTenant.TenantId = TenantBId;

            var tenantBFields = (await fieldGateway.GetAllAsync()).RightAsEnumerable().Single();

            Assert.IsTrue(tenantBFields.Any(f => f.Id == TenantBFieldId));
            Assert.IsFalse(tenantBFields.Any(f => f.Id == FieldId));
        }

        // A booking created by one tenant cannot be fetched by another.
        [TestMethod]
        public async Task GetBooking_FromAnotherTenant_ReturnsNotFound()
        {
            var request = new BookingRequest(
                "Default tenant booking",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                FieldId
            );

            var created = (await Controller.PostAsync(request)).Result as OkObjectResult;
            Assert.IsNotNull(created);
            var id = ((BookingResponse)created.Value!).Id;

            CurrentTenant.TenantId = TenantBId;

            var getResult = await Controller.GetAsync(id);
            Assert.IsInstanceOfType(getResult.Result, typeof(NotFoundResult));
        }

        // A booking cannot reference another tenant's field.
        [TestMethod]
        public async Task CreateBooking_AgainstAnotherTenantsField_IsRejected()
        {
            var request = new BookingRequest(
                "Cross-tenant booking",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                TenantBFieldId
            );

            var result = await Controller.PostAsync(request);

            Assert.IsNotInstanceOfType(result.Result, typeof(OkObjectResult));
        }

        // The same contact email exists once per tenant (seeded in SeedAdditionalAsync).
        [TestMethod]
        public async Task ContactEmail_IsUniquePerTenantNotGlobally()
        {
            var dbContext = Resolve<BookingApiDbContext>();

            var contactsWithSharedEmail = await dbContext
                .Contacts.IgnoreQueryFilters()
                .Where(c => c.Email == "test.contact@example.com")
                .ToListAsync();

            Assert.AreEqual(2, contactsWithSharedEmail.Count);
            CollectionAssert.AreEquivalent(
                new[] { TenantDb.DefaultTenantId, TenantBId },
                contactsWithSharedEmail.Select(c => c.TenantId).ToArray()
            );
        }

        // Entities created without an explicit TenantId are stamped with the acting tenant's.
        [TestMethod]
        public async Task CreatedEntities_AreStampedWithActingTenant()
        {
            var fieldGateway = Resolve<IFieldGateway>();

            var result = await fieldGateway.Create(
                new Field(Guid.NewGuid(), "Stamped Field", ImmutableArray<Pitch>.Empty)
            );
            // Create generates its own id; use the one it returns.
            var createdId = result.RightAsEnumerable().Single().Id;

            var dbContext = Resolve<BookingApiDbContext>();
            var stored = await dbContext
                .Fields.IgnoreQueryFilters()
                .SingleAsync(f => f.Id == createdId);

            Assert.AreEqual(TenantDb.DefaultTenantId, stored.TenantId);
        }
    }
}
