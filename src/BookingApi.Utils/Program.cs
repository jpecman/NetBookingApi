using BookingApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Utils
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string connectionString =
                Environment.GetEnvironmentVariable("CONNECTION_STRING")
                ?? throw new InvalidOperationException(
                    "CONNECTION_STRING environment variable is not set"
                );

            try
            {
                DbContextOptionsBuilder builder = new DbContextOptionsBuilder();

                var options = builder.UseNpgsql(connectionString).Options;

                BookingApiDbContext dbContext = new BookingApiDbContext(options);

                // dbContext.Fields.Add(new Infrastructure.DbEntities.FieldDb { Name = "Borovany Hlavni hriste" });
                // dbContext.Contacts.Add(new Infrastructure.DbEntities.ContactDb { FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Phone = "123 456 789"});

                /* dbContext.Bookings.Add(new Infrastructure.DbEntities.BookingDb
                {
                    Contact = dbContext.Contacts.First(),
                    Name = "Tambadooo",
                    Field = dbContext.Fields.Skip(1).First(),
                    Price = 1000,
                    Duration = new NpgsqlTypes.NpgsqlRange<DateTime>(DateTime.Today.AddHours(8).ToUniversalTime(), DateTime.Today.AddHours(9.5).ToUniversalTime())
                });
                */

                var booking = dbContext.Bookings.First();

                // booking.Price = 1000;

                await dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                Console.WriteLine("Entity not up to date.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Done !");
        }
    }
}
