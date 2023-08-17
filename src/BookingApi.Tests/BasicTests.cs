using BookingApi.Domain;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Tests.Fakes;
using System.Collections.Immutable;

namespace BookingApi.Tests
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public async Task BasicTestAsync()
        {
            Contact c = new Contact(Guid.Empty, "Jirka", "Sirka", "jpecman@gmail.com", "123 456 789");

            IBookingRepository repo = new FakeRepository();
            IBookingManager bookingManager = new BookingManager(repo);

            var today = DateTime.Today;

            var slots = ImmutableList.Create(new Slot(Guid.Empty, "Test 1", today.AddHours(10), today.AddHours(11.5), 1000, new Field(Guid.Empty, "Umelka")));

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
