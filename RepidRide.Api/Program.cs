using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RepidRide.Api.BackgroundServices;
using RepidRide.Api.Hubs;
using RepidRide.Api.Middleware;
using RepidRide.Core.Interfaces;
using RepidRide.Data.Repositories;
using RepidRide.Services;
using Serilog;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURE SERILOG FILE & CONSOLE LOGGING
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/rapidride-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

// 2. CONFIGURE CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 3. DATABASE REPOSITORIES
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=.\\SQLEXPRESS;Database=RepidRideDB;User Id=sumit;Password=Kun@123;TrustServerCertificate=True;MultipleActiveResultSets=True;";

builder.Services.AddScoped<ILocationRepository>(provider => 
    new LocationRepository(connectionString, provider.GetService<ILogger<LocationRepository>>()));

builder.Services.AddScoped<IUserRepository>(provider => 
    new UserRepository(connectionString, provider.GetService<ILogger<UserRepository>>()));

builder.Services.AddScoped<IRideRepository>(provider => 
    new RideRepository(connectionString, provider.GetService<ILogger<RideRepository>>()));

// 4. ROUTING SERVICES
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<IRoutingService, MockRoutingService>();
}
else
{
    builder.Services.AddHttpClient<IRoutingService, GoogleRoutingService>();
}

// 5. BACKGROUND WORKERS
builder.Services.AddHostedService<RideDispatcherWorker>();

// 6. JWT AUTHENTICATION
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "RepidRide_Super_Secret_Development_Key_2026_!@#";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/rideHub"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

var app = builder.Build();

// 7. PIPELINE CONFIGURATION
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<RideHub>("/rideHub");

try
{
    Log.Information("Rapid Ride Backend API starting up...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Rapid Ride Backend API terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}