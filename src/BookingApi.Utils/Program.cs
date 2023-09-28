using BookingApi.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BookingApi.Utils
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string connectionString = "Server=localhost;Port=5432;database=BookingDb;uid=bookaneer;pwd=***REMOVED***;";

            try
            {
                DbContextOptionsBuilder builder = new DbContextOptionsBuilder();

                var options = builder.UseNpgsql(connectionString).Options;

                BookingApiDbContext dbContext = new BookingApiDbContext(options);

                // dbContext.Fields.Add(new Infrastructure.DbEntities.FieldDb { Name = "Borovany Hlavni hriste" });
                // dbContext.Contacts.Add(new Infrastructure.DbEntities.ContactDb { FirstName = "Jiri", LastName = "Pecman", Email = "jpecman@gmail.com", Phone = "123 456 789"});

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
            catch (DbUpdateConcurrencyException dbUpdateEx)
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