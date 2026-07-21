using BookingApi.Application.Commands;
using BookingApi.Models.Bookings;
using BookingApi.Tests.Fixtures;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Tests
{
    [TestClass]
    public class BookingUpdateTests : BookingApiTestBase
    {
        private async Task<BookingResponse> CreateBookingAsync(
            string name,
            int hour,
            bool wholeField
        )
        {
            var request = new BookingRequest(
                name,
                DateTime.Now.Date.AddHours(hour),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                wholeField,
                ContactId,
                FieldId
            );

            var result = (await Controller.PostAsync(request)).Result as OkObjectResult;
            Assert.IsNotNull(result);
            return (BookingResponse)result.Value!;
        }

        // Renaming propagates to slot names; the contact can be reassigned.
        [TestMethod]
        public async Task UpdateBooking_ChangesNameContactAndSlotNames()
        {
            var booking = await CreateBookingAsync("Original", 10, wholeField: true);

            var result = await Controller.PutAsync(
                booking.Id,
                new UpdateBookingRequest("Renamed", OtherContactId)
            );
            var updated = (BookingResponse)((OkObjectResult)result.Result!).Value!;

            Assert.AreEqual("Renamed", updated.Name);
            Assert.AreEqual(OtherContactId, updated.Contact.Id);
            Assert.IsTrue(updated.Slots.All(s => s.Name == "Renamed"));
        }

        [TestMethod]
        public async Task UpdateBooking_UnknownContact_IsNotFound()
        {
            var booking = await CreateBookingAsync("Original", 10, wholeField: true);

            var result = await Controller.PutAsync(
                booking.Id,
                new UpdateBookingRequest("Renamed", Guid.NewGuid())
            );

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
        }

        // Moving a whole-field session moves the slot on every pitch.
        [TestMethod]
        public async Task UpdateSession_MovesAllSlotsOfTheSession()
        {
            var booking = await CreateBookingAsync("Move me", 10, wholeField: true);
            var from = booking.Slots[0].From;
            var to = booking.Slots[0].To;

            var result = await Controller.PutSessionAsync(
                booking.Id,
                new UpdateSessionRequest(from, to, from.AddHours(2), to.AddHours(2), 1500)
            );
            var updated = (BookingResponse)((OkObjectResult)result.Result!).Value!;

            Assert.AreEqual(2, updated.Slots.Length);
            Assert.IsTrue(updated.Slots.All(s => s.From == from.AddHours(2)));
            Assert.IsTrue(updated.Slots.All(s => s.To == to.AddHours(2)));
            Assert.IsTrue(updated.Slots.All(s => s.Price == 1500));
        }

        // Repricing without moving must not conflict with the session's own slots.
        [TestMethod]
        public async Task UpdateSession_PriceOnly_Succeeds()
        {
            var booking = await CreateBookingAsync("Reprice me", 10, wholeField: true);
            var from = booking.Slots[0].From;
            var to = booking.Slots[0].To;

            var result = await Controller.PutSessionAsync(
                booking.Id,
                new UpdateSessionRequest(from, to, from, to, 750)
            );
            var updated = (BookingResponse)((OkObjectResult)result.Result!).Value!;

            Assert.IsTrue(updated.Slots.All(s => s.Price == 750));
        }

        [TestMethod]
        public async Task UpdateSession_MovingOntoAnotherBooking_Conflicts()
        {
            var first = await CreateBookingAsync("First", 10, wholeField: true);
            var second = await CreateBookingAsync("Second", 14, wholeField: true);
            var from = second.Slots[0].From;
            var to = second.Slots[0].To;

            var result = await Controller.PutSessionAsync(
                second.Id,
                new UpdateSessionRequest(from, to, first.Slots[0].From, first.Slots[0].To, 1000)
            );

            Assert.IsInstanceOfType(result.Result, typeof(ConflictResult));
        }

        [TestMethod]
        public async Task UpdateSession_UnknownRange_IsNotFound()
        {
            var booking = await CreateBookingAsync("No such session", 10, wholeField: true);
            var from = booking.Slots[0].From.AddDays(1);
            var to = booking.Slots[0].To.AddDays(1);

            var result = await Controller.PutSessionAsync(
                booking.Id,
                new UpdateSessionRequest(from, to, from.AddHours(1), to.AddHours(1), 1000)
            );

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
        }
    }
}
