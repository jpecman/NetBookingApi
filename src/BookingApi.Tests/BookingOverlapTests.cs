using BookingApi.Application;
using BookingApi.Application.Commands;
using BookingApi.Models.Bookings;
using BookingApi.Tests.Fixtures;

using Microsoft.AspNetCore.Mvc;

namespace BookingApi.Tests
{
    [TestClass]
    public class BookingOverlapTests : BookingApiTestBase
    {
        // A new whole-field booking that partially overlaps an existing one is rejected.
        [TestMethod]
        public async Task CreateBooking_PartialOverlapSamePitch_ReturnsConflict()
        {
            var existing = new BookingRequest(
                "Existing",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour, // 10:00 - 11:30
                1000,
                true,
                ContactId,
                FieldId
            );
            Assert.IsInstanceOfType(
                (await Controller.PostAsync(existing)).Result,
                typeof(OkObjectResult)
            );

            var overlapping = new BookingRequest(
                "Overlapping",
                DateTime.Now.Date.AddHours(11), // 11:00 - 12:00, overlaps 11:00-11:30
                (int)BookingDurationType.OneHour,
                1000,
                true,
                ContactId,
                FieldId
            );

            var result = await Controller.PostAsync(overlapping);
            Assert.IsInstanceOfType(result.Result, typeof(ConflictObjectResult));
        }

        // Two whole-field bookings where one starts exactly when the other ends both succeed
        // (the time range is half-open, so touching endpoints don't count as overlapping).
        [TestMethod]
        public async Task CreateBooking_BackToBackWholeField_BothSucceed()
        {
            var first = new BookingRequest(
                "First",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour, // 10:00 - 11:30
                1000,
                true,
                ContactId,
                FieldId
            );
            var second = new BookingRequest(
                "Second",
                DateTime.Now.Date.AddHours(11).AddMinutes(30), // 11:30 - 13:00
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                FieldId
            );

            Assert.IsInstanceOfType(
                (await Controller.PostAsync(first)).Result,
                typeof(OkObjectResult)
            );
            Assert.IsInstanceOfType(
                (await Controller.PostAsync(second)).Result,
                typeof(OkObjectResult)
            );
        }

        // Two half-field bookings for the identical time window both succeed by landing on
        // different pitches.
        [TestMethod]
        public async Task CreateBooking_HalfField_DifferentPitchesSameWindow_BothSucceed()
        {
            var first = new BookingRequest(
                "First",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                false,
                ContactId,
                FieldId
            );
            var second = new BookingRequest(
                "Second",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                false,
                OtherContactId,
                FieldId
            );

            var firstResult = (await Controller.PostAsync(first)).Result as OkObjectResult;
            var secondResult = (await Controller.PostAsync(second)).Result as OkObjectResult;

            Assert.IsNotNull(firstResult);
            Assert.IsNotNull(secondResult);

            var firstPitchId = ((BookingResponse)firstResult.Value!).Slots.Single().PitchId;
            var secondPitchId = ((BookingResponse)secondResult.Value!).Slots.Single().PitchId;

            Assert.AreNotEqual(firstPitchId, secondPitchId);
        }

        // A half-field booking is rejected when an existing whole-field booking already
        // occupies both pitches for that window.
        [TestMethod]
        public async Task CreateBooking_HalfFieldOverlapsExistingWholeFieldBooking_ReturnsConflict()
        {
            var wholeField = new BookingRequest(
                "WholeField",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                FieldId
            );
            Assert.IsInstanceOfType(
                (await Controller.PostAsync(wholeField)).Result,
                typeof(OkObjectResult)
            );

            var halfField = new BookingRequest(
                "HalfField",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                false,
                OtherContactId,
                FieldId
            );

            var result = await Controller.PostAsync(halfField);
            Assert.IsInstanceOfType(result.Result, typeof(ConflictObjectResult));
        }

        // A half-field booking that overlaps an existing slot with a different start/end time
        // is rejected with the specific "must have the same start and end" alignment message.
        [TestMethod]
        public async Task CreateBooking_HalfFieldMisalignedOverlap_ReturnsConflictWithAlignmentMessage()
        {
            var bookingManager = Resolve<IBookingManager>();

            var existing = new CreateSingleBookingCommand(
                "Existing",
                DateTime.Now.Date.AddHours(10), // 10:00 - 11:30
                BookingDurationType.OneAndHalfHour,
                1000,
                false,
                ContactId,
                FieldId
            );
            var existingResult = await bookingManager.CreateAsync(existing);
            Assert.IsTrue(existingResult.IsRight);

            var misaligned = new CreateSingleBookingCommand(
                "Misaligned",
                DateTime.Now.Date.AddHours(10).AddMinutes(30), // 10:30 - 11:30, overlaps but different From
                BookingDurationType.OneHour,
                1000,
                false,
                OtherContactId,
                FieldId
            );
            var misalignedResult = await bookingManager.CreateAsync(misaligned);

            Assert.IsTrue(misalignedResult.IsLeft);
            misalignedResult.IfLeft(error =>
                Assert.IsTrue(
                    error.Message.Contains(
                        "must have the same start and end",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );
        }

        // A third aligned half-field booking is rejected once both pitches are already taken
        // for that window.
        [TestMethod]
        public async Task CreateBooking_HalfFieldNoPitchAvailable_ReturnsConflict()
        {
            var first = new BookingRequest(
                "First",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                false,
                ContactId,
                FieldId
            );
            var second = new BookingRequest(
                "Second",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                false,
                OtherContactId,
                FieldId
            );

            Assert.IsInstanceOfType(
                (await Controller.PostAsync(first)).Result,
                typeof(OkObjectResult)
            );
            Assert.IsInstanceOfType(
                (await Controller.PostAsync(second)).Result,
                typeof(OkObjectResult)
            );

            var third = new BookingRequest(
                "Third",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                false,
                ContactId,
                FieldId
            );

            var result = await Controller.PostAsync(third);
            Assert.IsInstanceOfType(result.Result, typeof(ConflictObjectResult));
        }

        // Cancelling a booking frees its pitch/time so an identical new booking can succeed.
        [TestMethod]
        public async Task CancelBooking_ThenRebookSameWindow_Succeeds()
        {
            var request = new BookingRequest(
                "Original",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                FieldId
            );

            var createResult = (await Controller.PostAsync(request)).Result as OkObjectResult;
            Assert.IsNotNull(createResult);
            var bookingId = ((BookingResponse)createResult.Value!).Id;

            var deleteResult = await Controller.DeleteAsync(bookingId);
            Assert.IsInstanceOfType(deleteResult, typeof(NoContentResult));

            var rebooking = new BookingRequest(
                "Rebooked",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                FieldId
            );

            var rebookResult = await Controller.PostAsync(rebooking);
            Assert.IsInstanceOfType(rebookResult.Result, typeof(OkObjectResult));
        }

        // A weekly-recurring booking with no conflicts creates a slot pair for every occurrence.
        [TestMethod]
        public async Task CreateBooking_RecurringWeekly_NoCollision_CreatesOneSlotPerOccurrence()
        {
            var from = DateTime.Now.Date.AddHours(10);
            var request = new BookingRequest(
                "Weekly",
                from,
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                FieldId,
                from.AddDays(14) // 3 weekly occurrences: from, from+7, from+14
            );

            var result = (await Controller.PostAsync(request)).Result as OkObjectResult;
            Assert.IsNotNull(result);

            var booking = (BookingResponse)result.Value!;
            Assert.AreEqual(6, booking.Slots.Length); // 3 occurrences * 2 pitches (whole field)
        }

        // If any single occurrence in a weekly-recurring booking collides, the whole booking is
        // rejected and none of its occurrences are persisted.
        [TestMethod]
        public async Task CreateBooking_RecurringWeekly_CollisionOnMiddleOccurrence_ReturnsConflictAndPersistsNothing()
        {
            var from = DateTime.Now.Date.AddHours(10);

            var preExisting = new BookingRequest(
                "PreExisting",
                from.AddDays(7), // collides with the 2nd weekly occurrence
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                OtherContactId,
                FieldId
            );
            Assert.IsInstanceOfType(
                (await Controller.PostAsync(preExisting)).Result,
                typeof(OkObjectResult)
            );

            var weekly = new BookingRequest(
                "Weekly",
                from,
                (int)BookingDurationType.OneAndHalfHour,
                1000,
                true,
                ContactId,
                FieldId,
                from.AddDays(14)
            );

            var result = await Controller.PostAsync(weekly);
            Assert.IsInstanceOfType(result.Result, typeof(ConflictObjectResult));

            var search = new SearchBookingRequest
            {
                From = from.AddDays(-1),
                To = from.AddDays(21),
                ContactId = ContactId,
            };
            var searchResult = (await Controller.GetAsync(search)).Result as OkObjectResult;
            Assert.IsNotNull(searchResult);
            Assert.AreEqual(0, ((BookingResponse[])searchResult.Value!).Length);
        }

        // A camp booking with several non-conflicting explicit sessions succeeds, creating a
        // slot pair per session.
        [TestMethod]
        public async Task CreateCampBooking_MultipleNonOverlappingSessions_Succeeds()
        {
            var from = DateTime.Now.Date;
            var request = new CampBookingRequest(
                "Camp",
                true,
                ContactId,
                FieldId,
                [
                    new SessionRequest(from.AddHours(9), 60, 500),
                    new SessionRequest(from.AddHours(11), 60, 500),
                    new SessionRequest(from.AddHours(13), 60, 500),
                ]
            );

            var result = (await Controller.PostCampAsync(request)).Result as OkObjectResult;
            Assert.IsNotNull(result);

            var booking = (BookingResponse)result.Value!;
            Assert.AreEqual(6, booking.Slots.Length); // 3 sessions * 2 pitches (whole field)
        }

        // If one session in a camp booking collides with an existing booking, the whole camp
        // booking is rejected and none of its sessions are persisted.
        [TestMethod]
        public async Task CreateCampBooking_OneSessionOverlapsExisting_ReturnsConflictAndPersistsNothing()
        {
            var from = DateTime.Now.Date;

            var preExisting = new BookingRequest(
                "PreExisting",
                from.AddHours(11),
                (int)BookingDurationType.OneHour,
                1000,
                true,
                OtherContactId,
                FieldId
            );
            Assert.IsInstanceOfType(
                (await Controller.PostAsync(preExisting)).Result,
                typeof(OkObjectResult)
            );

            var camp = new CampBookingRequest(
                "Camp",
                true,
                ContactId,
                FieldId,
                [
                    new SessionRequest(from.AddHours(9), 60, 500),
                    new SessionRequest(from.AddHours(11), 60, 500), // collides with pre-existing
                    new SessionRequest(from.AddHours(13), 60, 500),
                ]
            );

            var result = await Controller.PostCampAsync(camp);
            Assert.IsInstanceOfType(result.Result, typeof(ConflictObjectResult));

            var search = new SearchBookingRequest
            {
                From = from,
                To = from.AddDays(1),
                ContactId = ContactId,
            };
            var searchResult = (await Controller.GetAsync(search)).Result as OkObjectResult;
            Assert.IsNotNull(searchResult);
            Assert.AreEqual(0, ((BookingResponse[])searchResult.Value!).Length);
        }

        // A half-field camp booking with sessions on two different days picks a free pitch
        // independently for each day, proving the per-day conflict lookup doesn't leak across days.
        [TestMethod]
        public async Task CreateCampBooking_HalfField_SessionsOnDifferentDays_EachPicksFreePitchIndependently()
        {
            var day1 = DateTime.Now.Date.AddHours(10);
            var day2 = DateTime.Now.Date.AddDays(1).AddHours(10);

            // Book (by another contact) on both days first, so each day independently has one
            // pitch already taken - proving the per-day candidate lookup doesn't leak across days.
            var day1Existing = new BookingRequest(
                "Day1Existing",
                day1,
                (int)BookingDurationType.OneHour,
                1000,
                false,
                OtherContactId,
                FieldId
            );
            var day2Existing = new BookingRequest(
                "Day2Existing",
                day2,
                (int)BookingDurationType.OneHour,
                1000,
                false,
                OtherContactId,
                FieldId
            );

            var day1ExistingResult =
                (await Controller.PostAsync(day1Existing)).Result as OkObjectResult;
            var day2ExistingResult =
                (await Controller.PostAsync(day2Existing)).Result as OkObjectResult;
            Assert.IsNotNull(day1ExistingResult);
            Assert.IsNotNull(day2ExistingResult);

            var day1OccupiedPitchId = ((BookingResponse)day1ExistingResult.Value!).Slots
                .Single()
                .PitchId;
            var day2OccupiedPitchId = ((BookingResponse)day2ExistingResult.Value!).Slots
                .Single()
                .PitchId;

            var camp = new CampBookingRequest(
                "Camp",
                false,
                ContactId,
                FieldId,
                [new SessionRequest(day1, 60, 500), new SessionRequest(day2, 60, 500)]
            );

            var result = (await Controller.PostCampAsync(camp)).Result as OkObjectResult;
            Assert.IsNotNull(result);

            var booking = (BookingResponse)result.Value!;
            var orderedSlots = booking.Slots.OrderBy(s => s.From).ToArray();
            var day1Slot = orderedSlots[0]; // day1 is chronologically first
            var day2Slot = orderedSlots[1];

            Assert.AreNotEqual(day1OccupiedPitchId, day1Slot.PitchId);
            Assert.AreNotEqual(day2OccupiedPitchId, day2Slot.PitchId);
        }

        // A whole-field price is split across the pitches so the slot prices sum to it.
        [TestMethod]
        public async Task CreateBooking_WholeField_SplitsPriceAcrossPitches()
        {
            var request = new BookingRequest(
                "Split",
                DateTime.Now.Date.AddHours(10),
                (int)BookingDurationType.OneHour,
                1000.01m,
                true,
                ContactId,
                FieldId
            );

            var result = (await Controller.PostAsync(request)).Result as OkObjectResult;
            Assert.IsNotNull(result);

            var prices = ((BookingResponse)result.Value!).Slots.Select(s => s.Price).ToArray();
            Assert.AreEqual(2, prices.Length);
            Assert.AreEqual(1000.01m, prices.Sum());
            CollectionAssert.AreEquivalent(new[] { 500.01m, 500m }, prices);
        }

        // Two half-field sessions at the same time in one camp request land on different
        // pitches instead of both picking the first one.
        [TestMethod]
        public async Task CreateCampBooking_HalfField_TwoSessionsSameTime_UseBothPitches()
        {
            var from = DateTime.Now.Date.AddHours(10);
            var camp = new CampBookingRequest(
                "Camp",
                false,
                ContactId,
                FieldId,
                [new SessionRequest(from, 60, 500), new SessionRequest(from, 60, 500)]
            );

            var result = (await Controller.PostCampAsync(camp)).Result as OkObjectResult;
            Assert.IsNotNull(result);

            var pitchIds = ((BookingResponse)result.Value!).Slots.Select(s => s.PitchId);
            Assert.AreEqual(2, pitchIds.Distinct().Count());
        }

        // A third same-time session in the same request gets a clean no-pitch error rather
        // than a database constraint violation.
        [TestMethod]
        public async Task CreateCampBooking_HalfField_ThreeSessionsSameTime_ReturnsNoPitchError()
        {
            var bookingManager = Resolve<IBookingManager>();
            var from = DateTime.Now.Date.AddHours(10);

            var result = await bookingManager.CreateCampAsync(
                new CreateCampBookingCommand(
                    "Camp",
                    false,
                    ContactId,
                    FieldId,
                    [
                        new CampSession(from, 60, 500),
                        new CampSession(from, 60, 500),
                        new CampSession(from, 60, 500),
                    ]
                )
            );

            Assert.IsTrue(result.IsLeft);
            result.IfLeft(error => StringAssert.Contains(error.Message, "No available pitch"));
        }
    }
}
