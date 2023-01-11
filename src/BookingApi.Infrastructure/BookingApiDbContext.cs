using Microsoft.EntityFrameworkCore;

using BookingApi.Infrastructure.DbEntities;

namespace BookingApi.Infrastructure;

public class BookingApiDbContext : DbContext
{
    public BookingApiDbContext(DbContextOptions options) : base(options)
    { 
    }

    public DbSet<BookingDb> Bookings { get; set; }

    public DbSet<ContactDb> Contacts { get; set; }
}
