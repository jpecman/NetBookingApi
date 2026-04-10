using System.Text;

using BookingApi.Domain;
using BookingApi.Domain.Gateways;
using BookingApi.Domain.Services.LoginService;
using BookingApi.Infrastructure;
using BookingApi.Infrastructure.DbEntities;
using BookingApi.Infrastructure.Gateways;
using BookingApi.Infrastructure.Services.LoginService;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;



namespace BookingApi;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var configuration = builder.Configuration;

        var signingKey = configuration["SIGNING_KEY"];

        IdentityBuilder identityBuilder = builder.Services.AddIdentityCore<ApplicationUser>();
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
            });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddCors();

        string connectionString = configuration["CONNECTION_STRING"];

        builder.Services.AddDbContext<BookingApiDbContext>(options => options.UseNpgsql(connectionString));

        builder.Services.AddScoped<IBookingManager, BookingManager>();
        builder.Services.AddScoped<IBookingGateway, BookingGateway>();
        builder.Services.AddScoped<IFieldGateway, FieldGateway>();
        builder.Services.AddScoped<ISlotGateway, SlotGateway>();
        builder.Services.AddScoped<IContactGateway, ContactGateway>();
        builder.Services.AddScoped<IUserGateway, UserGateway>();
        builder.Services.AddScoped<IRoleGateway, RoleGateway>();
        builder.Services.AddScoped<ILoginService, LoginService>();

        builder.Services.AddSingleton(new LoginServiceOptions() { SigningKey = signingKey });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors(options => options
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());

#if !DEBUG
app.UseHttpsRedirection();
#endif
        app.UseAuthorization();

        app.MapControllers();

        using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        await MigrateDatabaseAsync(scope);
        await EnsureRolesAndAdminAccountAsync(scope, configuration);
        await SeedDataAsync(scope);

        app.Run();
    }

    private static async Task MigrateDatabaseAsync(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<BookingApiDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    private static async Task EnsureRolesAndAdminAccountAsync(IServiceScope scope, ConfigurationManager configuration)
    {
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var roleManager = scope.ServiceProvider.GetService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>();
        ILogger logger = loggerFactory.CreateLogger<Program>();

        await CreateRoleIfNeededAsync(logger, roleManager, UserRoles.Administrator);
        await CreateRoleIfNeededAsync(logger, roleManager, UserRoles.Content);

        // try to create the admin
        try
        {
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.Email == "jpecman@gmail.com");

            if (user == null)
            {
                user = new ApplicationUser() { Email = "jpecman@gmail.com", UserName = "jpecman@gmail.com" };

                var password = configuration["JPECMAN_PASSWORD"];

                await userManager.CreateAsync(user, password);
                var result = await userManager.AddToRoleAsync(user, UserRoles.Administrator);

                if (!result.Succeeded)
                {
                    logger.LogError("Failed to add user jpecman@gmail.com to Administrators");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add user jpecman@gmail.com to Administrators");
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
