using System.Text;
using System.Threading.RateLimiting;

using BookingApi.Application;
using BookingApi.Application.Services.LoginService;
using BookingApi.Domain.Gateways;
using BookingApi.Infrastructure;
using BookingApi.Infrastructure.DbEntities;
using BookingApi.Infrastructure.Gateways;
using BookingApi.Infrastructure.Services.LoginService;
using BookingApi.Infrastructure.Tenancy;

using BookingApi.Auth;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using OpenTelemetry.Logs;
using OpenTelemetry.Resources;



namespace BookingApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var configuration = builder.Configuration;

        var signingKey =
            configuration["SIGNING_KEY"]
            ?? throw new InvalidOperationException("SIGNING_KEY is not configured");

        // OTLP endpoint/protocol come from the standard OTEL_EXPORTER_OTLP_* env vars
        // (set in docker-compose.observability.yml).
        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.SetResourceBuilder(
                ResourceBuilder.CreateDefault().AddService(serviceName: "BookingApi")
            );
            options.AddOtlpExporter();
        });

        IdentityBuilder identityBuilder = builder.Services.AddIdentityCore<ApplicationUser>(
            // Longer minimum than the Identity default; mirrored in the PasswordRules
            // component of both SPAs — keep them in sync.
            options =>
            {
                options.Password.RequiredLength = 10;
                // Login and member lookup find users by email; a duplicate makes
                // FindByEmailAsync throw and locks the original owner out.
                options.User.RequireUniqueEmail = true;
            }
        );
        identityBuilder.AddRoles<IdentityRole>();
        identityBuilder.AddRoleValidator<RoleValidator<IdentityRole>>();
        identityBuilder.AddRoleManager<RoleManager<IdentityRole>>();
        identityBuilder.AddSignInManager<SignInManager<ApplicationUser>>();
        identityBuilder.AddEntityFrameworkStores<BookingApiDbContext>();

        builder.Services.AddControllers();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;

                SecurityKey securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(signingKey));

                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = false,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["token"];
                        return Task.CompletedTask;
                    },
                    // Revocation: reject tokens whose user, security stamp or
                    // membership no longer match the database.
                    OnTokenValidated = async context =>
                    {
                        var validator = context.HttpContext.RequestServices
                            .GetRequiredService<TokenSessionValidator>();

                        if (!await validator.IsCurrentAsync(context.Principal!))
                        {
                            context.Fail("Token is no longer valid.");
                        }
                    }
                };
            })
            .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationOptions.Scheme,
                _ => { }
            );

        builder.Services.AddAuthorization(options =>
            options.AddPolicy("ApiKeyOrJwt", policy =>
                policy
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, ApiKeyAuthenticationOptions.Scheme)
                    .RequireAuthenticatedUser()));

        // Login and password change: per client IP, against password spraying.
        // Behind nginx the IP comes from UseForwardedHeaders.
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("auth", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }
                )
            );
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddCors();

        string connectionString =
            configuration["CONNECTION_STRING"]
            ?? throw new InvalidOperationException("CONNECTION_STRING is not configured");

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ClaimsCurrentTenant>();
        builder.Services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<ClaimsCurrentTenant>());

        builder.Services.AddDbContext<BookingApiDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            
            if (builder.Environment.IsDevelopment())
                options.EnableSensitiveDataLogging();
        });

        builder.Services.AddScoped<IBookingManager, BookingManager>();
        builder.Services.AddScoped<ISlotManager, SlotManager>();
        builder.Services.AddScoped<IBookingGateway, BookingGateway>();
        builder.Services.AddScoped<IFieldGateway, FieldGateway>();
        builder.Services.AddScoped<IPitchGateway, PitchGateway>();
        builder.Services.AddScoped<ISlotGateway, SlotGateway>();
        builder.Services.AddScoped<IContactGateway, ContactGateway>();
        builder.Services.AddScoped<IUserGateway, UserGateway>();
        builder.Services.AddScoped<ITenantGateway, TenantGateway>();
        builder.Services.AddScoped<ILoginService, LoginService>();
        builder.Services.AddScoped<TokenSessionValidator>();

        builder.Services.AddSingleton(new LoginServiceOptions() { SigningKey = signingKey });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors(options => options
            .WithOrigins("http://localhost:5173", "http://localhost:5174")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());

#if !DEBUG
        // Behind nginx (which terminates TLS and forwards plain HTTP) the app would
        // otherwise see every request as HTTP and redirect-loop below. Trust the proxy's
        // headers: KnownProxies/KnownIPNetworks are cleared because the container sees
        // nginx as the Docker bridge gateway, not loopback, so the defaults never match.
        // Safe here - the api port is published on 127.0.0.1 only, so nginx on the same
        // host is the only thing that can reach it.
        var forwardedHeaders = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        };
        forwardedHeaders.KnownIPNetworks.Clear();
        forwardedHeaders.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardedHeaders);

        app.UseHttpsRedirection();
#endif
        // After UseForwardedHeaders, so limits are per real client IP.
        app.UseRateLimiter();
        app.UseAuthorization();

        app.MapControllers();

        using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        // Startup runs outside any request, so pin the tenant for seeding explicitly.
        scope.ServiceProvider.GetRequiredService<ClaimsCurrentTenant>().Use(TenantDb.DefaultTenantId);

        await MigrateDatabaseAsync(scope);
        await EnsureRolesAndAdminAccountAsync(scope, configuration);
        await EnsureDefaultTenantApiKeyAsync(scope, configuration);
        await SeedDataAsync(scope);

        app.Run();
    }

    private static async Task MigrateDatabaseAsync(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingApiDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    private static async Task EnsureRolesAndAdminAccountAsync(IServiceScope scope, ConfigurationManager configuration)
    {
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        ILogger logger = loggerFactory.CreateLogger<Program>();

        await CreateRoleIfNeededAsync(logger, roleManager, UserRoles.SuperAdmin);
        await CreateRoleIfNeededAsync(logger, roleManager, UserRoles.Administrator);
        await CreateRoleIfNeededAsync(logger, roleManager, UserRoles.Content);

        // try to create the admin
        var adminEmail = configuration["ADMIN_EMAIL"];

        try
        {
            if (string.IsNullOrEmpty(adminEmail))
            {
                throw new InvalidOperationException("ADMIN_EMAIL is not configured");
            }

            var user = await userManager.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

            if (user == null)
            {
                user = new ApplicationUser() { Email = adminEmail, UserName = adminEmail };

                var password =
                    configuration["ADMIN_PASSWORD"]
                    ?? throw new InvalidOperationException("ADMIN_PASSWORD is not configured");

                await userManager.CreateAsync(user, password);
                var result = await userManager.AddToRoleAsync(user, UserRoles.Administrator);

                if (!result.Succeeded)
                {
                    logger.LogError("Failed to add user {AdminEmail} to Administrators", adminEmail);
                }
            }

            // The seeded admin is also the platform operator.
            if (!await userManager.IsInRoleAsync(user, UserRoles.SuperAdmin))
            {
                await userManager.AddToRoleAsync(user, UserRoles.SuperAdmin);
            }

            await EnsureAdminMembershipAsync(scope, user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add user {AdminEmail} to Administrators", adminEmail);
        }

    }

    // The API_KEY env var stays authoritative for the default tenant's key until
    // key management moves to the admin app; other tenants get keys there.
    private static async Task EnsureDefaultTenantApiKeyAsync(
        IServiceScope scope,
        ConfigurationManager configuration
    )
    {
        var apiKey = configuration["API_KEY"];

        if (string.IsNullOrEmpty(apiKey))
        {
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<BookingApiDbContext>();
        var tenant = await db.Tenants.FirstAsync(t => t.Id == TenantDb.DefaultTenantId);
        var hash = ApiKeyHasher.Hash(apiKey);

        if (tenant.ApiKeyHash != hash)
        {
            tenant.ApiKeyHash = hash;
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureAdminMembershipAsync(IServiceScope scope, ApplicationUser user)
    {
        var db = scope.ServiceProvider.GetRequiredService<BookingApiDbContext>();

        if (!await db.TenantMemberships.AnyAsync(m => m.UserId == user.Id))
        {
            db.TenantMemberships.Add(
                new TenantMembershipDb
                {
                    UserId = user.Id,
                    TenantId = TenantDb.DefaultTenantId,
                    Role = UserRoles.Administrator,
                }
            );
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedDataAsync(IServiceScope scope)
    {
        var db = scope.ServiceProvider.GetRequiredService<BookingApiDbContext>();

        if (await db.Fields.AnyAsync())
        {
            return;
        }

        db.Fields.Add(new FieldDb
        {
            Id = Guid.NewGuid(),
            Name = "Main Field",
            Pitches =
            [
                new PitchDb { Id = Guid.NewGuid(), Name = "Pitch A" },
                new PitchDb { Id = Guid.NewGuid(), Name = "Pitch B" },
            ]
        });

        await db.SaveChangesAsync();
    }

    private static async Task CreateRoleIfNeededAsync(ILogger logger, RoleManager<IdentityRole> roleManager, string roleName)
    {
        try
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));

                if (!result.Succeeded)
                {
                    logger.LogError($"Failed to create role '{roleName}'");
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error when creating roles");
        }
    }
}
