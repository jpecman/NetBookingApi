using BookingApi.Application.Commands;
using BookingApi.Controllers;
using BookingApi.Models.Bookings;
using BookingApi.Tests.Fixtures;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Tests
{
    [TestClass]
    public class RangeLimitsTests : BookingApiTestBase
    {
        private static DateTime Start => DateTime.Now.Date.AddDays(1).AddHours(10);

        [TestMethod]
        public async Task Series_OfExactly52Sessions_IsCreated()
        {
            var result = await Controller.PostAsync(Series(Start.AddDays(7 * 51)));

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task Series_LongerThan52Sessions_IsRejected()
        {
            var result = await Controller.PostAsync(Series(Start.AddDays(7 * 52)));

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Camp_LongerThan52Weeks_IsRejected()
        {
            var request = Camp(sessionCount: 1, endDate: Start.AddDays(7 * 52));

            var result = await Controller.PostCampAsync(request);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task Camp_WithTooManySessionsPerWeek_IsRejected()
        {
            var request = Camp(sessionCount: 15, endDate: null);

            var result = await Controller.PostCampAsync(request);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task BookingSearch_LongerThanAYear_IsRejected()
        {
            var search = new SearchBookingRequest { From = Start, To = Start.AddDays(367) };

            var result = await Controller.GetAsync(search);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task BookingSearch_WithToBeforeFrom_IsRejected()
        {
            var search = new SearchBookingRequest { From = Start, To = Start.AddDays(-1) };

            var result = await Controller.GetAsync(search);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        [TestMethod]
        public async Task SlotSearch_LongerThanAYear_IsRejected()
        {
            var result = await Resolve<SlotController>()
                .GetAsync(Start, Start.AddDays(367), FieldId);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }

        private static BookingRequest Series(DateTime endDate) =>
            new(
                "Series",
                Start,
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                FieldId,
                endDate
            );

        // 90-minute sessions two hours apart, so they don't overlap.
        private static CampBookingRequest Camp(int sessionCount, DateTime? endDate) =>
            new(
                "Camp",
                true,
                ContactId,
                FieldId,
                Enumerable
                    .Range(0, sessionCount)
                    .Select(i => new SessionRequest(Start.AddHours(i * 2), 90, 100))
                    .ToList(),
                endDate
            );
    }
}
