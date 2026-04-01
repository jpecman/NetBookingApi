using System.Collections.Immutable;

using BookingApi.Domain.Commands;
using BookingApi.Domain.Entities;
using BookingApi.Domain.Repositories;
using BookingApi.Domain.Tests.Fakes;

namespace BookingApi.Domain.Tests
{
    [TestClass]
    public class BasicTests
    {
        private static readonly Guid _contactId = new("B1C9F8A7-5B3B-4E6D-9C2F-1A2B3C4D5E6F");

        private static readonly Guid _fieldId = new("DBD92AA4-27AE-4B8A-92C2-602417D66329");


        private static readonly Guid _firstHalfPitchId = new Guid("836CB57B-9C3F-4647-BA9D-C4A3428F2C91");
        private static readonly Guid _secondHalfPitchId = new Guid("EFADE869-AA09-4F76-8BC6-A6B21FB33CE0");

        [TestMethod]
        public async Task BasicTestAsync()
        {


            Contact c = new Contact(Guid.Empty, "Jirka", "Sirka", "jpecman@gmail.com", "123 456 789");

            IBookingRepository bookingRepo = new FakeBookingRepository();
            IFieldRepository fieldRepo = new FakeFieldRepository();
            IBookingManager bookingManager = new BookingManager(bookingRepo, fieldRepo);

            var today = DateTime.Today;
            var from = today.AddHours(10);
            var to = today.AddHours(11.5);

            var pitches = ImmutableArray.Create<Pitch>(new[]
            {
                new Pitch(_firstHalfPitchId, "First half", _fieldId, new ImmutableArray<Slot>()),
                new Pitch(_secondHalfPitchId, "Second half", _fieldId, new ImmutableArray<Slot>())
            });

            var field = new Field(_fieldId, "Umelka", pitches);

            var slots = ImmutableList.Create(
                    new Slot(
                        Guid.Empty, "Test 1", from, to, 1000, _firstHalfPitchId
                        ));

            var booking = CreateNewBooking("Test 1", from);

            var result = await bookingManager.CreateAsync(booking);

            Assert.IsTrue(result.IsRight);

            var duplicateBooking = CreateNewBooking("Test 1", from);

            result = await bookingManager.CreateAsync(duplicateBooking);

            Assert.IsTrue(result.IsLeft);
        }

        private CreateSingleBookingCommand CreateNewBooking(
            string name,
            DateTime from
        )
        {
            return new CreateSingleBookingCommand(name, from, BookingDurationType.OneAndHalfHour, 1500, true, _contactId, _fieldId);
        }
    }
}
