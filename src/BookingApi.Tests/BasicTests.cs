using BookingApi.Domain;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;

using Microsoft.Extensions.Configuration;

using System.Collections.Immutable;

namespace BookingApi.Tests
{
    [TestClass]
    public class BasicTests
    {
        private IServiceProvider _serviceProvider;

        [TestMethod]
        public async Task BasicTestAsync()
        {
            IConfigurationRoot configRoot = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        }
    }
}
