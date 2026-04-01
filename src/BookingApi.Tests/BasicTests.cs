using BookingApi.Controllers;
using BookingApi.Domain;
using BookingApi.Domain.Commands;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Infrastructure;
using BookingApi.Infrastructure.Repositories;
using BookingApi.Models.Bookings;
using BookingApi.Models.Contacts;
using BookingApi.Models.Fields;

using DotNet.Testcontainers.Builders;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Collections.Immutable;

using Testcontainers.PostgreSql;

namespace BookingApi.Tests
{
    [TestClass]
    public class BasicTests
    {
        private IServiceProvider _serviceProvider;
        private PostgreSqlContainer _container;

        [TestInitialize]
        public async Task Initialize()
        {
            IConfigurationRoot configRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            ServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<BookingController>();

            DbContextOptionsBuilder builder = new DbContextOptionsBuilder();

            string connectionString = "Server=localhost;Port=54321;database=BookingDb;uid=bookaneer;pwd=***REMOVED***;";
            var options = builder.UseNpgsql(connectionString).Options;

            services.AddSingleton(options);
            services.AddSingleton<BookingApiDbContext>();
            services.AddSingleton<IBookingRepository, BookingRepository>();
            services.AddSingleton<IBookingManager, BookingManager>();


            _serviceProvider = services.BuildServiceProvider();

            _container = new PostgreSqlBuilder()
                .WithDatabase("BookingDb")
                .WithUsername("bookaneer")
                .WithPassword("***REMOVED***")
                .WithPortBinding(54321, 5432)
                .Build();

            await _container.StartAsync();

            await MigrateDatabase(_serviceProvider);
        }

        [TestCleanup]
        public Task Cleanup()
        {
            return _container.StopAsync();
        }

        private async Task MigrateDatabase(IServiceProvider serviceProvider)
        {
            // update db to latest migration
            try
            {
                var dbContext = serviceProvider.GetService<BookingApiDbContext>();

                await dbContext.Database.MigrateAsync();

                dbContext.Fields.Add(new Infrastructure.DbEntities.FieldDb
                {
                    Id = _fieldId,
                    Name = "Field 1"
                });
                dbContext.Contacts.Add(new Infrastructure.DbEntities.ContactDb
                {
                    Id = _contactId,
                    FirstName = "Jirka",
                    LastName = "Sirka",
                    Email = "jpecman@gmail.com",
                    Phone = "776216469"
                });

                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static readonly Guid _fieldId = new("1E7AAE86-F1A9-4B39-85B1-778CACD5732E");
        private static readonly Guid _contactId = new("BE20E86E-D5B3-4BD6-A5A9-1B705D1FC88C");

        [TestMethod]
        public async Task BasicTestAsync()
        {
            var bookingController = _serviceProvider.GetRequiredService<BookingController>();

            var request = new BookingRequest("Test", DateTime.Now.Date.AddHours(10), (int) BookingDurationType.OneAndHalfHour, 1000, true, _contactId, _fieldId);

            var bookingResponse = await bookingController.PostAsync(request);

            var okObject = bookingResponse.Result as OkObjectResult;

            Assert.IsNotNull(okObject);
            Assert.IsNotNull(okObject.Value);

            var getSaved = await bookingController.GetAsync(Guid.Parse(okObject.Value.ToString()));

            okObject = getSaved.Result as OkObjectResult;

            Assert.IsNotNull(okObject);
            Assert.IsNotNull(okObject.Value);
        }
    }
}
