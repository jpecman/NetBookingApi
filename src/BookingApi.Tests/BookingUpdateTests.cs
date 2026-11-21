using BookingApi.Application.Commands;
using BookingApi.Controllers;
using BookingApi.Models.Bookings;
using BookingApi.Models.Slots;
using BookingApi.Tests.Fixtures;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Tests
{
    [TestClass]
    public class BookingUpdateTests : BookingApiTestBase
    {
        private SlotController SlotController => Resolve<SlotController>();

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

        private static SlotUpdateRequest ToUpdate(
            SlotResponse slot,
            string? name = null,
            decimal? price = null
        ) => new(slot.Id, name ?? slot.Name, price ?? slot.Price);

        private async Task<ActionResult<SlotResponse[]>> PatchAsync(
            DateTime from,
            DateTime to,
            params SlotUpdateRequest[] slots
        ) => await SlotController.PatchAsync(new UpdateSlotsRequest(from, to, slots));

        // Keeps the slots at their current time.
        private Task<ActionResult<SlotResponse[]>> PatchInPlaceAsync(
            SlotResponse at,
            params SlotUpdateRequest[] slots
        ) => PatchAsync(at.From, at.To, slots);

        private static SlotResponse[] Ok(ActionResult<SlotResponse[]> result) =>
            (SlotResponse[])((OkObjectResult)result.Result!).Value!;

        // Slots still carrying the booking name follow a rename; the contact can be reassigned.
        [TestMethod]
        public async Task UpdateBooking_UncustomisedSlots_FollowRename()
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
        public async Task UpdateBooking_CustomSlotName_WithoutFlag_Conflicts()
        {
            var booking = await CreateBookingAsync("Original", 10, wholeField: true);
            Ok(await PatchInPlaceAsync(booking.Slots[0], ToUpdate(booking.Slots[0], name: "Custom")));

            var result = await Controller.PutAsync(
                booking.Id,
                new UpdateBookingRequest("Renamed", ContactId)
            );

            Assert.IsInstanceOfType(result.Result, typeof(ConflictObjectResult));

            var reloaded = (BookingResponse)
                ((OkObjectResult)(await Controller.GetAsync(booking.Id)).Result!).Value!;
            Assert.AreEqual("Original", reloaded.Name);
            Assert.IsTrue(reloaded.Slots.Any(s => s.Name == "Custom"));
        }

        [TestMethod]
        public async Task UpdateBooking_CustomSlotName_WithFlag_Overwrites()
        {
            var booking = await CreateBookingAsync("Original", 10, wholeField: true);
            Ok(await PatchInPlaceAsync(booking.Slots[0], ToUpdate(booking.Slots[0], name: "Custom")));

            var result = await Controller.PutAsync(
                booking.Id,
                new UpdateBookingRequest("Renamed", ContactId, OverwriteSlotNames: true)
            );
            var updated = (BookingResponse)((OkObjectResult)result.Result!).Value!;

            Assert.IsTrue(updated.Slots.All(s => s.Name == "Renamed"));
        }

        // Changing only the contact never touches slot names, so it can't conflict.
        [TestMethod]
        public async Task UpdateBooking_ContactOnly_WithCustomSlotName_Succeeds()
        {
            var booking = await CreateBookingAsync("Original", 10, wholeField: true);
            Ok(await PatchInPlaceAsync(booking.Slots[0], ToUpdate(booking.Slots[0], name: "Custom")));

            var result = await Controller.PutAsync(
                booking.Id,
                new UpdateBookingRequest("Original", OtherContactId)
            );
            var updated = (BookingResponse)((OkObjectResult)result.Result!).Value!;

            Assert.AreEqual(OtherContactId, updated.Contact.Id);
            Assert.IsTrue(updated.Slots.Any(s => s.Name == "Custom"));
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

        // Without isWholeField, moving a whole-field session means sending the slot on every pitch.
        [TestMethod]
        public async Task PatchSlots_MovesWholeFieldSession()
        {
            var booking = await CreateBookingAsync("Move me", 10, wholeField: true);
            var from = booking.Slots[0].From.AddHours(2);
            var to = booking.Slots[0].To.AddHours(2);

            var updated = Ok(
                await PatchAsync(
                    from,
                    to,
                    booking.Slots.Select(s => ToUpdate(s, price: 700)).ToArray()
                )
            );

            Assert.AreEqual(2, updated.Length);
            Assert.IsTrue(updated.All(s => s.From == from));
            Assert.IsTrue(updated.All(s => s.To == to));
            Assert.IsTrue(updated.All(s => s.Price == 700));
        }

        // Repricing without moving must not conflict with the session's own slots.
        [TestMethod]
        public async Task PatchSlots_PriceOnly_Succeeds()
        {
            var booking = await CreateBookingAsync("Reprice me", 10, wholeField: true);

            var updated = Ok(
                await PatchInPlaceAsync(booking.Slots[0], ToUpdate(booking.Slots[0], price: 750))
            );

            Assert.AreEqual(750, updated.Single().Price);
        }

        [TestMethod]
        public async Task PatchSlots_DifferentNamesPerSlot_AreKept()
        {
            var booking = await CreateBookingAsync("Names", 10, wholeField: true);

            var updated = Ok(
                await PatchInPlaceAsync(
                    booking.Slots[0],
                    ToUpdate(booking.Slots[0], name: "Team A"),
                    ToUpdate(booking.Slots[1], name: "Team B")
                )
            );

            CollectionAssert.AreEquivalent(
                new[] { "Team A", "Team B" },
                updated.Select(s => s.Name).ToArray()
            );
        }

        [TestMethod]
        public async Task PatchSlots_MovingOntoAnotherBooking_Conflicts()
        {
            var first = await CreateBookingAsync("First", 10, wholeField: true);
            var second = await CreateBookingAsync("Second", 14, wholeField: true);

            var result = await PatchAsync(
                first.Slots[0].From,
                first.Slots[0].To,
                second.Slots.Select(s => ToUpdate(s)).ToArray()
            );

            Assert.IsInstanceOfType(result.Result, typeof(ConflictObjectResult));
        }

        // A half-field slot moved so it partly overlaps the other pitch's session breaks the
        // field alignment invariant.
        [TestMethod]
        public async Task PatchSlots_MisalignedHalfFieldMove_Conflicts()
        {
            await CreateBookingAsync("Other half", 10, wholeField: false);
            var booking = await CreateBookingAsync("Mine", 14, wholeField: false);
            var slot = booking.Slots.Single();
            var from = DateTime.Now.Date.AddHours(10).AddMinutes(30);

            var result = await PatchAsync(from, from.AddHours(1), ToUpdate(slot));

            Assert.IsInstanceOfType(result.Result, typeof(ConflictObjectResult));
        }

        [TestMethod]
        public async Task PatchSlots_UnknownId_IsNotFound()
        {
            var booking = await CreateBookingAsync("Unknown", 10, wholeField: true);

            var result = await PatchInPlaceAsync(
                booking.Slots[0],
                ToUpdate(booking.Slots[0]) with { Id = Guid.NewGuid() }
            );

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task PatchSlots_CancelledSlot_IsBadRequest()
        {
            var booking = await CreateBookingAsync("Cancelled", 10, wholeField: true);
            await SlotController.DeleteAsync(booking.Slots[0].Id);

            var result = await PatchInPlaceAsync(
                booking.Slots[0],
                ToUpdate(booking.Slots[0], price: 1)
            );

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
        }
    }
}
