using BookingApi.Common.Messaging;
using BookingApi.Domain.Gateways;
using BookingApi.Infrastructure;
using BookingApi.Infrastructure.Gateways;
using BookingApi.Infrastructure.Tenancy;
using BookingApi.Tests.Fixtures;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingApi.Tests
{
    [TestClass]
    public class TenantProvisioningTests
    {
        private const string Password = "Sup3r!Secret";

        private ServiceProvider _provider = null!;
        private ITenantGateway _gateway = null!;

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
                .AddEntityFrameworkStores<BookingApiDbContext>();
            services.AddScoped<ITenantGateway, TenantGateway>();

            _provider = services.BuildServiceProvider();
            _gateway = _provider.GetRequiredService<ITenantGateway>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _provider.Dispose();
        }

        [TestMethod]
        public async Task InviteNewUser_CreatesFlaggedUserWithTemporaryPassword()
        {
            var tenant = (
                await _gateway.CreateAsync($"Club {Guid.NewGuid():N}")
            ).RightAsEnumerable().Single();
            var email = UniqueEmail();

            var invite = (
                await _gateway.AddMemberAsync(tenant.Id, email, UserRoles.Administrator)
            ).RightAsEnumerable().Single();

            Assert.AreEqual(email, invite.Member.Email);
            Assert.AreEqual(UserRoles.Administrator, invite.Member.Role);
            Assert.IsNotNull(invite.TemporaryPassword);

            var userManager = _provider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(invite.Member.UserId);
            Assert.IsNotNull(user);
            Assert.IsTrue(user.MustChangePassword);
            Assert.IsTrue(await userManager.CheckPasswordAsync(user, invite.TemporaryPassword));

            var members = (await _gateway.GetMembersAsync(tenant.Id)).RightAsEnumerable().Single();
            Assert.AreEqual(1, members.Length);
        }

        [TestMethod]
        public async Task InviteExistingUser_AttachesMembershipWithoutTemporaryPassword()
        {
            var tenant = (
                await _gateway.CreateAsync($"Club {Guid.NewGuid():N}")
            ).RightAsEnumerable().Single();
            var email = UniqueEmail();

            var userManager = _provider.GetRequiredService<UserManager<ApplicationUser>>();
            var created = await userManager.CreateAsync(
                new ApplicationUser { UserName = email, Email = email },
                Password
            );
            Assert.IsTrue(created.Succeeded);

            var invite = (
                await _gateway.AddMemberAsync(tenant.Id, email, UserRoles.Content)
            ).RightAsEnumerable().Single();

            Assert.AreEqual(UserRoles.Content, invite.Member.Role);
            Assert.IsNull(invite.TemporaryPassword);
        }

        [TestMethod]
        public async Task InviteUser_AlreadyInAnotherTenant_Conflicts()
        {
            var tenantA = (
                await _gateway.CreateAsync($"Club {Guid.NewGuid():N}")
            ).RightAsEnumerable().Single();
            var tenantB = (
                await _gateway.CreateAsync($"Club {Guid.NewGuid():N}")
            ).RightAsEnumerable().Single();
            var email = UniqueEmail();

            var first = await _gateway.AddMemberAsync(tenantA.Id, email, UserRoles.Content);
            Assert.IsTrue(first.IsRight);

            var second = await _gateway.AddMemberAsync(tenantB.Id, email, UserRoles.Content);

            var error = second.LeftAsEnumerable().Single();
            Assert.AreEqual(ErrorType.Conflict, error.ErrorType, error.Message);
        }

        [TestMethod]
        public async Task UnassignableRole_IsInvalid()
        {
            var tenant = (
                await _gateway.CreateAsync($"Club {Guid.NewGuid():N}")
            ).RightAsEnumerable().Single();

            var result = await _gateway.AddMemberAsync(
                tenant.Id,
                UniqueEmail(),
                UserRoles.SuperAdmin
            );

            Assert.AreEqual(ErrorType.Invalid, result.LeftAsEnumerable().Single().ErrorType);
        }

        [TestMethod]
        public async Task SetMemberRole_UpdatesRole()
        {
            var tenant = (
                await _gateway.CreateAsync($"Club {Guid.NewGuid():N}")
            ).RightAsEnumerable().Single();
            var invite = (
                await _gateway.AddMemberAsync(tenant.Id, UniqueEmail(), UserRoles.Content)
            ).RightAsEnumerable().Single();

            var updated = await _gateway.SetMemberRoleAsync(
                tenant.Id,
                invite.Member.UserId,
                UserRoles.Administrator
            );

            Assert.AreEqual(
                UserRoles.Administrator,
                updated.RightAsEnumerable().Single().Role
            );
        }

        [TestMethod]
        public async Task RemoveMember_RemovesMembership()
        {
            var tenant = (
                await _gateway.CreateAsync($"Club {Guid.NewGuid():N}")
            ).RightAsEnumerable().Single();
            var invite = (
                await _gateway.AddMemberAsync(tenant.Id, UniqueEmail(), UserRoles.Content)
            ).RightAsEnumerable().Single();

            var removed = await _gateway.RemoveMemberAsync(tenant.Id, invite.Member.UserId);
            Assert.IsTrue(removed.IsRight);

            var members = (await _gateway.GetMembersAsync(tenant.Id)).RightAsEnumerable().Single();
            Assert.AreEqual(0, members.Length);
        }

        [TestMethod]
        public async Task ResetMemberPassword_IssuesTemporaryPasswordAndFlagsUser()
        {
            var tenant = (
                await _gateway.CreateAsync($"Club {Guid.NewGuid():N}")
            ).RightAsEnumerable().Single();
            var invite = (
                await _gateway.AddMemberAsync(tenant.Id, UniqueEmail(), UserRoles.Content)
            ).RightAsEnumerable().Single();

            // Simulate the user having completed their first login.
            var userManager = _provider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await userManager.FindByIdAsync(invite.Member.UserId))!;
            user.MustChangePassword = false;
            await userManager.UpdateAsync(user);

            var temporaryPassword = (
                await _gateway.ResetMemberPasswordAsync(tenant.Id, invite.Member.UserId)
            ).RightAsEnumerable().Single();

            Assert.AreNotEqual(invite.TemporaryPassword, temporaryPassword);

            user = (await userManager.FindByIdAsync(invite.Member.UserId))!;
            Assert.IsTrue(user.MustChangePassword);
            Assert.IsTrue(await userManager.CheckPasswordAsync(user, temporaryPassword));
        }

        [TestMethod]
        public async Task ResetMemberPassword_ForNonMember_IsNotFound()
        {
            var tenant = (
                await _gateway.CreateAsync($"Club {Guid.NewGuid():N}")
            ).RightAsEnumerable().Single();

            var result = await _gateway.ResetMemberPasswordAsync(tenant.Id, Guid.NewGuid().ToString());

            Assert.AreEqual(ErrorType.NotFound, result.LeftAsEnumerable().Single().ErrorType);
        }

        [TestMethod]
        public async Task ResetApiKey_StoresHashOfReturnedKey()
        {
            var tenant = (
                await _gateway.CreateAsync($"Club {Guid.NewGuid():N}")
            ).RightAsEnumerable().Single();

            var key = (await _gateway.ResetApiKeyAsync(tenant.Id)).RightAsEnumerable().Single();

            var dbContext = _provider.GetRequiredService<BookingApiDbContext>();
            var stored = await dbContext.Tenants.SingleAsync(t => t.Id == tenant.Id);

            Assert.AreEqual(ApiKeyHasher.Hash(key), stored.ApiKeyHash);
            Assert.IsTrue((await _gateway.GetAsync(tenant.Id)).RightAsEnumerable().Single().HasApiKey);
        }

        private static string UniqueEmail() => $"provision-{Guid.NewGuid():N}@example.com";
    }
}
