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
            DateOnly someDaty = DateOnly.FromDateTime(DateTime.Today);
            TimeOnly time = TimeOnly.FromDateTime(DateTime.Now);

            Contact c = new Contact(Guid.Empty, "Jirka", "Sirka", "jpecman@gmail.com", "123 456 789");

            IBookingRepository repo = new FakeRepository();
            IBookingManager bookingManager = new BookingManager(repo);

            var today = DateTime.Today;

            var booking = CreateNewBooking(
                "Test 1",
                c,
                null
            );

            var result = await bookingManager.CreateAsync(booking);

            Assert.IsTrue(result.IsRight);

            var duplicateBooking = CreateNewBooking(
                "Test 1",
                c,
                null
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
