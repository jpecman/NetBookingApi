using System.Text;

using BookingApi.Domain;
using BookingApi.Domain.Repositories;
using BookingApi.Domain.Services.LoginService;
using BookingApi.Infrastructure;
using BookingApi.Infrastructure.Repositories;
using BookingApi.Infrastructure.Services.LoginService;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var signingKey = builder.Configuration["SigningKey"];

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

string connectionString = builder.Configuration["CONNECTION_STRING"];

builder.Services.AddDbContext<BookingApiDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IBookingManager, BookingManager>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IFieldRepository, FieldRepository>();
builder.Services.AddScoped<ILoginService, LoginService>();


builder.Services.AddOptions();
builder.Services.Configure<LoginServiceOptions>(options => new LoginServiceOptions { SigningKey = signingKey });

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
