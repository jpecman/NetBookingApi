using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using BookingApi.Application.Services.LoginService;
using BookingApi.Infrastructure;
using BookingApi.Infrastructure.DbEntities;
using BookingApi.Infrastructure.Services.LoginService;
using BookingApi.Infrastructure.Tenancy;
using BookingApi.Tests.Fixtures;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BookingApi.Tests
{
    [TestClass]
    public class TokenSessionValidatorTests
    {
        private const string Password = "Sup3r!Secret";
        private const string SigningKey = "integration-test-signing-key-0123456789abcdef";

        private ServiceProvider _provider = null!;

        [TestInitialize]
        public void Initialize()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpContextAccessor();
            services.AddAuthentication();
            services.AddSingleton<ICurrentTenant>(new TestCurrentTenant());
            services.AddDbContext<BookingApiDbContext>(o =>
                o.UseNpgsql(PostgresAssemblyFixture.ConnectionString)
            );
            services
                .AddIdentityCore<ApplicationUser>(o => o.Password.RequiredLength = 10)
                .AddRoles<IdentityRole>()
                .AddSignInManager<SignInManager<ApplicationUser>>()
                .AddEntityFrameworkStores<BookingApiDbContext>();
            services.AddSingleton(
                new LoginServiceOptions
                {
                    SigningKey = SigningKey,
                    TokenLifetime = TimeSpan.FromHours(8),
                }
            );
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<TokenSessionValidator>();

            _provider = services.BuildServiceProvider();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _provider.Dispose();
        }

        [TestMethod]
        public async Task FreshToken_IsCurrent()
        {
            var (_, principal) = await LoginAsync();

            Assert.IsTrue(await Validator.IsCurrentAsync(principal));
        }

        [TestMethod]
        public async Task AfterPasswordChange_TokenIsRejected()
        {
            var (user, principal) = await LoginAsync();

            var change = await UserManager.ChangePasswordAsync(user, Password, "N3w!Sup3r!Secret");
            Assert.IsTrue(change.Succeeded);

            Assert.IsFalse(await Validator.IsCurrentAsync(principal));
        }

        [TestMethod]
        public async Task AfterAdminPasswordReset_TokenIsRejected()
        {
            var (user, principal) = await LoginAsync();

            // Same calls as TenantGateway.ResetMemberPasswordAsync.
            await UserManager.RemovePasswordAsync(user);
            await UserManager.AddPasswordAsync(user, "T3mp!Passw0rd");

            Assert.IsFalse(await Validator.IsCurrentAsync(principal));
        }

        [TestMethod]
        public async Task AfterMembershipRemoved_TokenIsRejected()
        {
            var (user, principal) = await LoginAsync();

            var dbContext = _provider.GetRequiredService<BookingApiDbContext>();
            var membership = await dbContext.TenantMemberships.SingleAsync(m => m.UserId == user.Id);
            dbContext.TenantMemberships.Remove(membership);
            await dbContext.SaveChangesAsync();

            Assert.IsFalse(await Validator.IsCurrentAsync(principal));
        }

        [TestMethod]
        public async Task AfterRoleChange_TokenIsRejected()
        {
            var (user, principal) = await LoginAsync();

            var dbContext = _provider.GetRequiredService<BookingApiDbContext>();
            var membership = await dbContext.TenantMemberships.SingleAsync(m => m.UserId == user.Id);
            membership.Role = UserRoles.Content;
            await dbContext.SaveChangesAsync();

            Assert.IsFalse(await Validator.IsCurrentAsync(principal));
        }

        [TestMethod]
        public async Task AfterUserDeleted_TokenIsRejected()
        {
            var (user, principal) = await LoginAsync();

            var result = await UserManager.DeleteAsync(user);
            Assert.IsTrue(result.Succeeded);

            Assert.IsFalse(await Validator.IsCurrentAsync(principal));
        }

        [TestMethod]
        public async Task TokenWithoutStamp_IsRejected()
        {
            var (_, principal) = await LoginAsync();

            var withoutStamp = new ClaimsPrincipal(
                new ClaimsIdentity(
                    principal.Claims.Where(c => c.Type != LoginClaims.SecurityStamp),
                    "test"
                )
            );

            Assert.IsFalse(await Validator.IsCurrentAsync(withoutStamp));
        }

        private TokenSessionValidator Validator =>
            _provider.GetRequiredService<TokenSessionValidator>();

        private UserManager<ApplicationUser> UserManager =>
            _provider.GetRequiredService<UserManager<ApplicationUser>>();

        private async Task<(ApplicationUser User, ClaimsPrincipal Principal)> LoginAsync()
        {
            var email = $"session-test-{Guid.NewGuid():N}@example.com";
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

            var login = await _provider
                .GetRequiredService<ILoginService>()
                .LoginAsync(new LoginRequestModel(email, Password));
            var token = login.RightAsEnumerable().Single().Token;

            return (user, ToPrincipal(token));
        }

        // Validates with the app's own parameters, including the inbound
        // claim-type mapping (nameid -> NameIdentifier, role -> Role).
        private ClaimsPrincipal ToPrincipal(string token) =>
            new JwtSecurityTokenHandler().ValidateToken(
                token,
                _provider.GetRequiredService<LoginServiceOptions>().CreateValidationParameters(),
                out _
            );
    }
}
