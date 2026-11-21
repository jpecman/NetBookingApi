using BookingApi.Application.Commands;
using BookingApi.Controllers;
using BookingApi.Models.Bookings;
using BookingApi.Models.Slots;
using BookingApi.Tests.Fixtures;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Tests
{
    // Switching a session between half and whole field through PATCH /slots.
    [TestClass]
    public class SlotFieldSizeTests : BookingApiTestBase
    {
        private SlotController SlotController => Resolve<SlotController>();

        private static DateTime At(int hour) => DateTime.Now.Date.AddDays(1).AddHours(hour);

        private async Task<BookingResponse> CreateAsync(string name, int hour, bool wholeField)
        {
            var request = new BookingRequest(
                name,
                At(hour),
                (int)BookingDurationType.OneHour,
                1000,
                wholeField,
                ContactId,
                FieldId
            );
            var result = (await Controller.PostAsync(request)).Result as OkObjectResult;
            Assert.IsNotNull(result);
            return (BookingResponse)result.Value!;
        }

        private async Task<BookingResponse> ReloadAsync(Guid id) =>
            (BookingResponse)((OkObjectResult)(await Controller.GetAsync(id)).Result!).Value!;

        private Task<ActionResult<SlotResponse[]>> PatchAsync(
            DateTime from,
            params SlotUpdateRequest[] slots
        ) => SlotController.PatchAsync(new UpdateSlotsRequest(from, from.AddHours(1), slots));

        private static SlotResponse[] Ok(ActionResult<SlotResponse[]> result) =>
            (SlotResponse[])((OkObjectResult)result.Result!).Value!;

        [TestMethod]
        public async Task WholeToHalf_KeepsGivenSlot_DeletesOther()
        {
            var booking = await CreateAsync("Whole", 10, wholeField: true);
            var kept = booking.Slots[0];

            var updated = Ok(
                await PatchAsync(At(12), new SlotUpdateRequest(kept.Id, "Half", 400, false))
            );

            var slot = updated.Single();
            Assert.AreEqual(kept.Id, slot.Id);
            Assert.AreEqual(kept.PitchId, slot.PitchId);
            Assert.AreEqual(400, slot.Price);
            Assert.AreEqual(At(12).ToUniversalTime(), slot.From.ToUniversalTime());

            var reloaded = await ReloadAsync(booking.Id);
            Assert.AreEqual(kept.Id, reloaded.Slots.Single().Id);
        }

        [TestMethod]
        public async Task HalfToWhole_CreatesSlotOnFreePitch_SplitsPrice()
        {
            var booking = await CreateAsync("Half", 10, wholeField: false);
            var half = booking.Slots.Single();

            var updated = Ok(
                await PatchAsync(At(10), new SlotUpdateRequest(half.Id, "Whole", 1001, true))
            );

            Assert.AreEqual(2, updated.Length);
            CollectionAssert.AreEquivalent(
                new[] { PitchAId, PitchBId },
                updated.Select(s => s.PitchId).ToArray()
            );
            Assert.AreEqual(1001, updated.Sum(s => s.Price));
            Assert.IsTrue(updated.All(s => s.BookingId == booking.Id && s.Name == "Whole"));

            var reloaded = await ReloadAsync(booking.Id);
            Assert.AreEqual(2, reloaded.Slots.Length);
        }

        [TestMethod]
        public async Task WholeWithFlag_MovesAndSplitsPriceOverBothSlots()
        {
            var booking = await CreateAsync("Whole", 10, wholeField: true);

            var updated = Ok(
                await PatchAsync(
                    At(14),
                    new SlotUpdateRequest(booking.Slots[0].Id, "Moved", 700, true)
                )
            );

            CollectionAssert.AreEquivalent(
                booking.Slots.Select(s => s.Id).ToArray(),
                updated.Select(s => s.Id).ToArray()
            );
            Assert.IsTrue(updated.All(s => s.Price == 350 && s.Name == "Moved"));
            Assert.IsTrue(
                updated.All(s => s.From.ToUniversalTime() == At(14).ToUniversalTime())
            );
        }

        // "Two halves": the other pitch belongs to another booking.
        [TestMethod]
        public async Task HalfToWhole_OtherPitchTaken_Conflicts()
        {
            var mine = await CreateAsync("Mine", 10, wholeField: false);
            await CreateAsync("Other", 10, wholeField: false);

            var result = await PatchAsync(
                At(10),
                new SlotUpdateRequest(mine.Slots.Single().Id, "Mine", 1000, true)
            );

            Assert.IsInstanceOfType(result.Result, typeof(ConflictObjectResult));
            Assert.AreEqual(1, (await ReloadAsync(mine.Id)).Slots.Length);
        }

        [TestMethod]
        public async Task FlagWithOtherSlotOfSameSession_IsBadRequest()
        {
            var booking = await CreateAsync("Whole", 10, wholeField: true);

            var result = await PatchAsync(
                At(10),
                new SlotUpdateRequest(booking.Slots[0].Id, "Whole", 500, false),
                new SlotUpdateRequest(booking.Slots[1].Id, "Whole", 500)
            );

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult));
            Assert.AreEqual(2, (await ReloadAsync(booking.Id)).Slots.Length);
        }

        [TestMethod]
        public async Task NoFlag_UpdatesOnlyGivenSlot()
        {
            var booking = await CreateAsync("Whole", 10, wholeField: true);
            var slot = booking.Slots[0];

            var updated = Ok(
                await PatchAsync(At(10), new SlotUpdateRequest(slot.Id, "Whole", 750))
            );

            Assert.AreEqual(750, updated.Single().Price);
            var reloaded = await ReloadAsync(booking.Id);
            Assert.AreEqual(2, reloaded.Slots.Length);
            Assert.AreEqual(1250, reloaded.Slots.Sum(s => s.Price));
        }
    }
}
