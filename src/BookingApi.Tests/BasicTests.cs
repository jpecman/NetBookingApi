using BookingApi.Controllers;
using BookingApi.Application;
using BookingApi.Application.Commands;
using BookingApi.Domain.Gateways;
using BookingApi.Infrastructure;
using BookingApi.Infrastructure.DbEntities;
using BookingApi.Infrastructure.Gateways;
using BookingApi.Models.Bookings;
using BookingApi.Models.Contacts;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Testcontainers.PostgreSql;

namespace BookingApi.Tests
{
    [TestClass]
    public class BasicTests
    {
        private IServiceProvider _serviceProvider;
        private PostgreSqlContainer _container;

        private static readonly Guid _fieldId = new("1E7AAE86-F1A9-4B39-85B1-778CACD5732E");
        private static readonly Guid _contactId = new("BE20E86E-D5B3-4BD6-A5A9-1B705D1FC88C");
        private static readonly Guid _otherContactId = new("C1D2E3F4-A5B6-7890-BC12-DE34FA567890");
        private static readonly Guid _pitchAId = new("A3C1D2E4-F5B6-7890-AB12-CD34EF567890");
        private static readonly Guid _pitchBId = new("B4D2E3F5-A6C7-8901-BC23-DE45FA678901");

        [TestInitialize]
        public async Task Initialize()
        {
            _container = new PostgreSqlBuilder()
                .WithDatabase("BookingDb")
                .WithUsername("bookaneer")
                .WithPassword("***REMOVED***")
                .Build();

            await _container.StartAsync();

            var options = new DbContextOptionsBuilder()
                .UseNpgsql(_container.GetConnectionString())
                .Options;

            ServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(options);
            services.AddSingleton<BookingApiDbContext>();
            services.AddSingleton<IBookingGateway, BookingGateway>();
            services.AddSingleton<IFieldGateway, FieldGateway>();
            services.AddSingleton<IBookingManager, BookingManager>();
            services.AddSingleton<BookingController>();

            _serviceProvider = services.BuildServiceProvider();

            await MigrateDatabaseAsync();
        }

        [TestCleanup]
        public Task Cleanup() => _container.StopAsync();

        private async Task MigrateDatabaseAsync()
        {
            var dbContext = _serviceProvider.GetRequiredService<BookingApiDbContext>();

            await dbContext.Database.MigrateAsync();

            dbContext.Fields.Add(new FieldDb
            {
                Id = _fieldId,
                Name = "Field 1",
                Pitches =
                [
                    new PitchDb { Id = _pitchAId, Name = "Pitch A", FieldId = _fieldId },
                    new PitchDb { Id = _pitchBId, Name = "Pitch B", FieldId = _fieldId },
                ]
            });
            dbContext.Contacts.Add(new ContactDb
            {
                Id = _contactId,
                FirstName = "Jirka",
                LastName = "Sirka",
                Email = "jpecman@gmail.com",
                Phone = "776216469"
            });
            dbContext.Contacts.Add(new ContactDb
            {
                Id = _otherContactId,
                FirstName = "Other",
                LastName = "Contact",
                Email = "other@example.com",
                Phone = "776216470"
            });

            await dbContext.SaveChangesAsync();
        }

        [TestMethod]
        public async Task CreateBooking_ReturnsBookingAndCanBeRetrieved()
        {
            var controller = _serviceProvider.GetRequiredService<BookingController>();

            var request = new BookingRequest("Test", DateTime.Now.Date.AddHours(10), (int)BookingDurationType.OneAndHalfHour, 1000, true, _contactId, _fieldId);

            var createResult = await controller.PostAsync(request);
            var created = createResult.Result as OkObjectResult;

            Assert.IsNotNull(created);
            Assert.IsNotNull(created.Value);

            var id = ((BookingResponse)created.Value).Id;
            var getResult = await controller.GetAsync(id);
            var fetched = getResult.Result as OkObjectResult;

            Assert.IsNotNull(fetched);
            Assert.IsNotNull(fetched.Value);
        }

        [TestMethod]
        public async Task CreateBooking_DuplicateOverlappingSlot_IsRejected()
        {
            var controller = _serviceProvider.GetRequiredService<BookingController>();

            var request = new BookingRequest("First", DateTime.Now.Date.AddHours(10), (int)BookingDurationType.OneAndHalfHour, 1000, true, _contactId, _fieldId);

            var firstResult = await controller.PostAsync(request);
            Assert.IsInstanceOfType(firstResult.Result, typeof(OkObjectResult));

            var duplicate = new BookingRequest("Duplicate", DateTime.Now.Date.AddHours(10), (int)BookingDurationType.OneAndHalfHour, 1000, true, _contactId, _fieldId);

            var secondResult = await controller.PostAsync(duplicate);
            Assert.IsNotInstanceOfType(secondResult.Result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task GetAsync_FilteredByContactId_ReturnsOnlyThatContactsBookings()
        {
            var controller = _serviceProvider.GetRequiredService<BookingController>();

            var ownRequest = new BookingRequest("Own", DateTime.Now.Date.AddHours(10), (int)BookingDurationType.OneAndHalfHour, 1000, true, _contactId, _fieldId);
            var otherRequest = new BookingRequest("Other", DateTime.Now.Date.AddHours(14), (int)BookingDurationType.OneAndHalfHour, 1000, true, _otherContactId, _fieldId);

            Assert.IsInstanceOfType((await controller.PostAsync(ownRequest)).Result, typeof(OkObjectResult));
            Assert.IsInstanceOfType((await controller.PostAsync(otherRequest)).Result, typeof(OkObjectResult));

            var search = new SearchBookingRequest
            {
                From = DateTime.Now.Date,
                To = DateTime.Now.Date.AddDays(1),
                ContactId = _contactId
            };

            var searchResult = await controller.GetAsync(search);
            var okResult = searchResult.Result as OkObjectResult;

            Assert.IsNotNull(okResult);
            var bookings = (BookingResponse[])okResult.Value!;

            Assert.AreEqual(1, bookings.Length);
            Assert.AreEqual(_contactId, bookings[0].Contact.Id);
        }
    }
}
