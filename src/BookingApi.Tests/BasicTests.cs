using BookingApi.Domain;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Tests.Fakes;

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

            IBookingRepository repo = new FakeRepository();
            IBookingManager bookingManager = new BookingManager(repo);

            var today = DateTime.Today;

            var booking = CreateNewBooking(
                "Test 1",
                DateOnly.FromDateTime(today),
                new TimeOnly(9),
                new TimeOnly(10, 30),
                1500
            );

            var result = await bookingManager.Create(booking);

            Assert.IsTrue(result.IsRight);

            var duplicateBooking = CreateNewBooking(
                "Test 1",
                DateOnly.FromDateTime(today),
                new TimeOnly(10),
                new TimeOnly(11, 30),
                1500
            );

            result = await bookingManager.Create(duplicateBooking);

            Assert.IsTrue(result.IsLeft);
        }

        private Booking CreateNewBooking(
            string name,
            DateOnly day,
            TimeOnly from,
            TimeOnly to,
            decimal price
        )
        {
            return new Booking(Guid.Empty, name, day, from, to, price);
        }
    }
}
