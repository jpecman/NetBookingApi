using System.Collections.Immutable;

using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Domain.Tests.Fakes;

namespace BookingApi.Domain.Tests
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public async Task BasicTestAsync()
        {
            Guid fieldId = new Guid("DBD92AA4-27AE-4B8A-92C2-602417D66329");

            Guid firstHalfPitchId = new Guid("836CB57B-9C3F-4647-BA9D-C4A3428F2C91");
            Guid secondHalfPitchId = new Guid("EFADE869-AA09-4F76-8BC6-A6B21FB33CE0");

            Contact c = new Contact(Guid.Empty, "Jirka", "Sirka", "jpecman@gmail.com", "123 456 789");

            IBookingRepository repo = new FakeRepository();
            IBookingManager bookingManager = new BookingManager(repo);

            var today = DateTime.Today;

            var pitches = ImmutableArray.Create<Pitch>(new[]
            {
                new Pitch(firstHalfPitchId, "First half", fieldId, new ImmutableArray<Slot>()),
                new Pitch(secondHalfPitchId, "Second half", fieldId, new ImmutableArray<Slot>())
            });

            var field = new Field(fieldId, "Umelka", pitches);

            var slots = ImmutableList.Create(
                    new Slot(
                        Guid.Empty, "Test 1", today.AddHours(10), today.AddHours(11.5), 1000, firstHalfPitchId
                        ));

            var booking = CreateNewBooking(
                "Test 1",
                c,
                slots
            );

            var result = await bookingManager.CreateAsync(booking);

            Assert.IsTrue(result.IsRight);

            var duplicateBooking = CreateNewBooking(
                "Test 1",
                c,
                slots
            );

            result = await bookingManager.CreateAsync(duplicateBooking);

            Assert.IsTrue(result.IsLeft);
        }

        private Booking CreateNewBooking(
            string name,
            Contact contact,
            ImmutableList<Slot> slots
        )
        {
            return new Booking(Guid.Empty, name, contact, slots);
        }
    }
}
