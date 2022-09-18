using Microsoft.EntityFrameworkCore;

using BookingApi.Domain.Entities;

namespace BookingApi.Infrastructure;

public class BookingApiDbContext : DbContext
{
    public DbSet<Booking> Bookings { get; set; }
}
