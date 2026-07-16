using BookingApi.Application.Commands;
using BookingApi.Models.Bookings;
using BookingApi.Tests.Fixtures;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Tests
{
    [TestClass]
    public class BookingControllerTests : BookingApiTestBase
    {
        // A booking created via POST can immediately be fetched back by id.
        [TestMethod]
        public async Task CreateBooking_ReturnsBookingAndCanBeRetrieved()
        {
            var request = new BookingRequest(
                "Test",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                FieldId
            );

            var createResult = await Controller.PostAsync(request);
            var created = createResult.Result as OkObjectResult;

            Assert.IsNotNull(created);
            Assert.IsNotNull(created.Value);

            var id = ((BookingResponse)created.Value).Id;
            var getResult = await Controller.GetAsync(id);
            var fetched = getResult.Result as OkObjectResult;

            Assert.IsNotNull(fetched);
            Assert.IsNotNull(fetched.Value);
        }

        // Posting the exact same whole-field time slot twice rejects the second request.
        [TestMethod]
        public async Task CreateBooking_DuplicateOverlappingSlot_IsRejected()
        {
            var request = new BookingRequest(
                "First",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                FieldId
            );

            var firstResult = await Controller.PostAsync(request);
            Assert.IsInstanceOfType(firstResult.Result, typeof(OkObjectResult));

            var duplicate = new BookingRequest(
                "Duplicate",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                FieldId
            );

            var secondResult = await Controller.PostAsync(duplicate);
            Assert.IsNotInstanceOfType(secondResult.Result, typeof(OkObjectResult));
        }

        // Searching with a ContactId filter excludes bookings belonging to other contacts.
        [TestMethod]
        public async Task GetAsync_FilteredByContactId_ReturnsOnlyThatContactsBookings()
        {
            var ownRequest = new BookingRequest(
                "Own",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                FieldId
            );
            var otherRequest = new BookingRequest(
                "Other",
                DateTime.Now.Date.AddHours(14),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                OtherContactId,
                FieldId
            );

            Assert.IsInstanceOfType(
                (await Controller.PostAsync(ownRequest)).Result,
                typeof(OkObjectResult)
            );
            Assert.IsInstanceOfType(
                (await Controller.PostAsync(otherRequest)).Result,
                typeof(OkObjectResult)
            );

            var search = new SearchBookingRequest
            {
                From = DateTime.Now.Date,
                To = DateTime.Now.Date.AddDays(1),
                ContactId = ContactId,
            };

            var searchResult = await Controller.GetAsync(search);
            var okResult = searchResult.Result as OkObjectResult;

            Assert.IsNotNull(okResult);
            var bookings = (BookingResponse[])okResult.Value!;

            Assert.AreEqual(1, bookings.Length);
            Assert.AreEqual(ContactId, bookings[0].Contact.Id);
        }
    }
}
