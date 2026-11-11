using Microsoft.EntityFrameworkCore;

using BookingApi.Infrastructure.DbEntities;
using BookingApi.Infrastructure.Tenancy;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace BookingApi.Infrastructure;

public class BookingApiDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentTenant _currentTenant;

    public BookingApiDbContext(DbContextOptions options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<TenantDb> Tenants { get; set; }

    public DbSet<TenantMembershipDb> TenantMemberships { get; set; }

    public DbSet<BookingDb> Bookings { get; set; }

    public DbSet<ContactDb> Contacts { get; set; }

    public DbSet<FieldDb> Fields { get; set; }

    public DbSet<PitchDb> Pitches { get; set; }

    public DbSet<SlotDb> Slots { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity creates EmailIndex as non-unique; back RequireUniqueEmail with
        // a database constraint so concurrent creates can't slip a duplicate in.
        builder.Entity<ApplicationUser>(user =>
            user.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique()
        );

        builder.Entity<TenantDb>(tenant =>
        {
            tenant.Property(t => t.Name).HasMaxLength(100);
            tenant.Property(t => t.ApiKeyHash).HasMaxLength(64);
            tenant.HasIndex(t => t.ApiKeyHash).IsUnique();
        });

        builder.Entity<TenantMembershipDb>(membership =>
        {
            membership.HasKey(m => new { m.UserId, m.TenantId });
            membership.Property(m => m.Role).HasMaxLength(50);
            // One tenant per user for now; drop this index to allow multi-tenant users.
            membership.HasIndex(m => m.UserId).IsUnique();
            membership
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            membership
                .HasOne<TenantDb>()
                .WithMany()
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureTenantOwned<BookingDb>(builder);
        ConfigureTenantOwned<ContactDb>(builder);
        ConfigureTenantOwned<FieldDb>(builder);
        ConfigureTenantOwned<PitchDb>(builder);
        ConfigureTenantOwned<SlotDb>(builder);

        builder.Entity<ContactDb>(contact =>
        {
            contact.Property(c => c.FirstName).HasMaxLength(30);
            contact.Property(c => c.LastName).HasMaxLength(30);
            contact.Property(c => c.Email).HasMaxLength(50);
            contact.Property(c => c.Phone).HasMaxLength(20);

            contact
                .HasIndex(c => new { c.TenantId, c.Email })
                .IsUnique()
                .HasFilter("\"Email\" <> ''");
        });
    }

    private void ConfigureTenantOwned<T>(ModelBuilder builder)
        where T : class, ITenantOwned
    {
        builder.Entity<T>(entity =>
        {
            entity
                .HasOne<TenantDb>()
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.TenantId);
            // EF.Property instead of e.TenantId: interface member access does not
            // translate inside query filters.
            entity.HasQueryFilter(e =>
                EF.Property<Guid>(e, nameof(ITenantOwned.TenantId)) == _currentTenant.TenantId
            );
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTenantId();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        StampTenantId();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampTenantId()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantOwned>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = _currentTenant.TenantId;
            }
        }
    }
}

public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// True while the account's password was chosen by someone else (admin
    /// created the user or reset a forgotten password): login is refused until
    /// the user sets their own.
    /// </summary>
    public bool MustChangePassword { get; set; }
}
