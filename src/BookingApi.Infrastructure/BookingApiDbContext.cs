using Microsoft.EntityFrameworkCore;

using BookingApi.Infrastructure.DbEntities;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace BookingApi.Infrastructure;

public class BookingApiDbContext : IdentityDbContext<ApplicationUser>
{
    public BookingApiDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<BookingDb> Bookings { get; set; }

    public DbSet<ContactDb> Contacts { get; set; }

    public DbSet<FieldDb> Fields { get; set; }

    public DbSet<SlotDb> Slots { get; set; }
}

public class ApplicationUser : IdentityUser
{
}
