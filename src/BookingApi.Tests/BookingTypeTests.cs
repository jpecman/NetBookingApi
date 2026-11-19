using BookingApi.Application.Commands;
using BookingApi.Domain.Entities;
using BookingApi.Models.Bookings;
using BookingApi.Tests.Fixtures;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Tests
{
    [TestClass]
    public class BookingTypeTests : BookingApiTestBase
    {
        private async Task<BookingResponse> CreateAsync(DateTime from, DateTime? endDate)
        {
            var request = new BookingRequest(
                "Booking",
                from,
                (int)BookingDurationType.OneHour,
                1000,
                true,
                ContactId,
                FieldId,
                endDate
            );
            var result = (await Controller.PostAsync(request)).Result as OkObjectResult;
            Assert.IsNotNull(result);
            return (BookingResponse)result.Value!;
        }

        [TestMethod]
        public async Task CreateBooking_NoEndDate_IsSingle()
        {
            var booking = await CreateAsync(DateTime.Now.Date.AddHours(10), null);
            Assert.AreEqual(BookingType.Single, booking.Type);
        }

        [TestMethod]
        public async Task CreateBooking_WithEndDate_IsRepeated()
        {
            var from = DateTime.Now.Date.AddHours(10);
            var booking = await CreateAsync(from, from.AddDays(14));
            Assert.AreEqual(BookingType.Repeated, booking.Type);
        }

        // An EndDate within the first week produces one session, so the booking is Single.
        [TestMethod]
        public async Task CreateBooking_EndDateWithinFirstWeek_IsSingle()
        {
            var from = DateTime.Now.Date.AddHours(10);
            var booking = await CreateAsync(from, from.AddDays(3));
            Assert.AreEqual(BookingType.Single, booking.Type);
        }

        [TestMethod]
        public async Task CreateCampBooking_IsCamp()
        {
            var from = DateTime.Now.Date;
            var camp = new CampBookingRequest(
                "Camp",
                true,
                ContactId,
                FieldId,
                [new SessionRequest(from.AddHours(9), 60, 500)],
                from.AddDays(7).AddHours(9)
            );

            var result = (await Controller.PostCampAsync(camp)).Result as OkObjectResult;
            Assert.IsNotNull(result);

            var bookings = (BookingResponse[])result.Value!;
            Assert.AreEqual(2, bookings.Length);
            Assert.IsTrue(bookings.All(b => b.Type == BookingType.Camp));
        }

        // Search returns all of a booking's slots, not only those in the search window.
        [TestMethod]
        public async Task Search_ReturnsSlotsOutsideWindow()
        {
            var from = DateTime.Now.Date.AddHours(10);
            await CreateAsync(from, from.AddDays(14)); // 3 occurrences * 2 pitches

            var search = new SearchBookingRequest
            {
                From = from.AddHours(-1),
                To = from.AddDays(1), // covers the first occurrence only
                ContactId = ContactId,
            };
            var searchResult = (await Controller.GetAsync(search)).Result as OkObjectResult;
            Assert.IsNotNull(searchResult);

            var booking = ((BookingResponse[])searchResult.Value!).Single();
            Assert.AreEqual(6, booking.Slots.Length);
            Assert.AreEqual(BookingType.Repeated, booking.Type);
        }
    }
}
