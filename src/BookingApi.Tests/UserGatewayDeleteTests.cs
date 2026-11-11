using BookingApi.Common.Messaging;
using BookingApi.Domain.Gateways;
using BookingApi.Infrastructure;
using BookingApi.Infrastructure.DbEntities;
using BookingApi.Infrastructure.Gateways;
using BookingApi.Infrastructure.Tenancy;
using BookingApi.Tests.Fixtures;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingApi.Tests
{
    [TestClass]
    public class UserGatewayDeleteTests
    {
        private const string Password = "Sup3r!Secret";

        private ServiceProvider _provider = null!;
        private IUserGateway _gateway = null!;

        [TestInitialize]
        public void Initialize()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<ICurrentTenant>(new TestCurrentTenant());
            services.AddDbContext<BookingApiDbContext>(o =>
                o.UseNpgsql(PostgresAssemblyFixture.ConnectionString)
            );
            services
                .AddIdentityCore<ApplicationUser>(o => o.Password.RequiredLength = 10)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<BookingApiDbContext>();
            services.AddScoped<IUserGateway, UserGateway>();

            _provider = services.BuildServiceProvider();
            _gateway = _provider.GetRequiredService<IUserGateway>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _provider.Dispose();
        }

        [TestMethod]
        public async Task Delete_OtherMember_Succeeds()
        {
            var admin = await CreateMemberAsync();
            var user = await CreateMemberAsync();

            var result = await _gateway.DeleteAsync(user.Id, admin.Id);

            Assert.IsTrue(result.IsRight);
            Assert.IsNull(await UserManager.FindByIdAsync(user.Id));
        }

        [TestMethod]
        public async Task Delete_Self_IsRefused()
        {
            var admin = await CreateMemberAsync();

            var result = await _gateway.DeleteAsync(admin.Id, admin.Id);

            Assert.AreEqual(ErrorType.Invalid, result.LeftAsEnumerable().Single().ErrorType);
            Assert.IsNotNull(await UserManager.FindByIdAsync(admin.Id));
        }

        [TestMethod]
        public async Task Delete_SuperAdmin_IsRefused()
        {
            var admin = await CreateMemberAsync();
            var superAdmin = await CreateMemberAsync();

            var roleManager = _provider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roleManager.RoleExistsAsync(UserRoles.SuperAdmin))
            {
                await roleManager.CreateAsync(new IdentityRole(UserRoles.SuperAdmin));
            }
            await UserManager.AddToRoleAsync(superAdmin, UserRoles.SuperAdmin);

            var result = await _gateway.DeleteAsync(superAdmin.Id, admin.Id);

            Assert.AreEqual(ErrorType.Invalid, result.LeftAsEnumerable().Single().ErrorType);
            Assert.IsNotNull(await UserManager.FindByIdAsync(superAdmin.Id));
        }

        private UserManager<ApplicationUser> UserManager =>
            _provider.GetRequiredService<UserManager<ApplicationUser>>();

        private async Task<ApplicationUser> CreateMemberAsync()
        {
            var email = $"delete-test-{Guid.NewGuid():N}@example.com";
            var user = new ApplicationUser { UserName = email, Email = email };

            var create = await UserManager.CreateAsync(user, Password);
            Assert.IsTrue(
                create.Succeeded,
                string.Join("; ", create.Errors.Select(e => e.Description))
            );

            var dbContext = _provider.GetRequiredService<BookingApiDbContext>();
            dbContext.TenantMemberships.Add(
                new TenantMembershipDb
                {
                    UserId = user.Id,
                    TenantId = TenantDb.DefaultTenantId,
                    Role = UserRoles.Administrator,
                }
            );
            await dbContext.SaveChangesAsync();

            return user;
        }
    }
}
