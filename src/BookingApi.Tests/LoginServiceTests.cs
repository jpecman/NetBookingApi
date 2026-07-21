using System.IdentityModel.Tokens.Jwt;

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
    public class LoginServiceTests
    {
        private const string Password = "Sup3r!Secret";

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
                    SigningKey = "integration-test-signing-key-0123456789abcdef",
                    TokenLifetime = TimeSpan.FromHours(8),
                }
            );
            services.AddScoped<ILoginService, LoginService>();

            _provider = services.BuildServiceProvider();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _provider.Dispose();
        }

        [TestMethod]
        public async Task Login_WithMembership_IssuesTokenWithTenantAndRoleClaims()
        {
            var user = await CreateUserAsync();
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

            var result = await _provider
                .GetRequiredService<ILoginService>()
                .LoginAsync(new LoginRequestModel(user.Email!, Password));

            var response = result.RightAsEnumerable().Single();
            var token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);

            Assert.AreEqual(
                TenantDb.DefaultTenantId.ToString(),
                token.Claims.Single(c => c.Type == TenantClaims.TenantId).Value
            );
            Assert.IsTrue(token.Claims.Any(c => c.Value == UserRoles.Administrator));

            var expectedExpiry = DateTime.UtcNow.AddHours(8);
            Assert.IsTrue(
                (token.ValidTo - expectedExpiry).Duration() < TimeSpan.FromMinutes(10),
                $"Token expiry {token.ValidTo:O} not within 8 hours of now"
            );
        }

        [TestMethod]
        public async Task Login_WithoutMembership_IsRefused()
        {
            var user = await CreateUserAsync();

            var result = await _provider
                .GetRequiredService<ILoginService>()
                .LoginAsync(new LoginRequestModel(user.Email!, Password));

            Assert.IsTrue(result.IsLeft);
            Assert.AreEqual(
                LoginError.NoTenantMembership,
                result.LeftAsEnumerable().Single()
            );
        }

        [TestMethod]
        public async Task Login_WhenPasswordChangeRequired_IsRefusedWithThatError()
        {
            var user = await CreateUserAsync(mustChangePassword: true);
            await AddMembershipAsync(user);

            var result = await _provider
                .GetRequiredService<ILoginService>()
                .LoginAsync(new LoginRequestModel(user.Email!, Password));

            Assert.AreEqual(
                LoginError.PasswordChangeRequired,
                result.LeftAsEnumerable().Single()
            );
        }

        [TestMethod]
        public async Task ChangePassword_ClearsFlagAndCompletesLogin()
        {
            var user = await CreateUserAsync(mustChangePassword: true);
            await AddMembershipAsync(user);
            var service = _provider.GetRequiredService<ILoginService>();

            var result = await service.ChangePasswordAsync(
                new ChangePasswordRequestModel(user.Email!, Password, "N3w!Sup3r!Secret")
            );

            var response = result.RightAsEnumerable().Single();
            var token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
            Assert.AreEqual(
                TenantDb.DefaultTenantId.ToString(),
                token.Claims.Single(c => c.Type == TenantClaims.TenantId).Value
            );

            // Subsequent logins with the new password work without the flag.
            var login = await service.LoginAsync(
                new LoginRequestModel(user.Email!, "N3w!Sup3r!Secret")
            );
            Assert.IsTrue(login.IsRight);
        }

        [TestMethod]
        public async Task ChangePassword_WithWrongCurrentPassword_IsRefused()
        {
            var user = await CreateUserAsync(mustChangePassword: true);
            await AddMembershipAsync(user);

            var result = await _provider
                .GetRequiredService<ILoginService>()
                .ChangePasswordAsync(
                    new ChangePasswordRequestModel(user.Email!, "Wr0ng!Passw0rd", "N3w!Sup3r!Secret")
                );

            Assert.IsTrue(result.IsLeft);
        }

        [TestMethod]
        public async Task ChangePassword_WithTooShortNewPassword_IsRejected()
        {
            var user = await CreateUserAsync(mustChangePassword: true);
            await AddMembershipAsync(user);

            var result = await _provider
                .GetRequiredService<ILoginService>()
                .ChangePasswordAsync(
                    new ChangePasswordRequestModel(user.Email!, Password, "Sh0rt!aa")
                );

            Assert.AreEqual(
                LoginError.InvalidNewPassword,
                result.LeftAsEnumerable().Single()
            );
        }

        private async Task AddMembershipAsync(ApplicationUser user)
        {
            var dbContext = _provider.GetRequiredService<BookingApiDbContext>();
            dbContext.TenantMemberships.Add(
                new TenantMembershipDb
                {
                    UserId = user.Id,
                    TenantId = TenantDb.DefaultTenantId,
                    Role = UserRoles.Content,
                }
            );
            await dbContext.SaveChangesAsync();
        }

        private async Task<ApplicationUser> CreateUserAsync(bool mustChangePassword = false)
        {
            var email = $"login-test-{Guid.NewGuid():N}@example.com";
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                MustChangePassword = mustChangePassword,
            };

            var result = await _provider
                .GetRequiredService<UserManager<ApplicationUser>>()
                .CreateAsync(user, Password);
            Assert.IsTrue(
                result.Succeeded,
                string.Join("; ", result.Errors.Select(e => e.Description))
            );

            return user;
        }
    }
}
