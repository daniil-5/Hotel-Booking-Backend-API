using System.Text;
using System.Text.Json.Serialization;
using BookingSystem.Application.Decorators;
using BookingSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Services;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Interfaces;
using BookingSystem.Domain.Other;
using Dapper;

using BookingSystem.Infrastructure.Repositories;
using BookingSystem.Infrastructure.Services;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;
using BookingSystem.Domain.Enums;
using BookingSystem.Infrastructure.Messaging;
using Scalar.AspNetCore;
using Serilog.Events;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

#region Logging and Controllers

// Configure Serilog for logging
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "Logs/app-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
        .Enrich.FromLogContext()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
});

// Add controllers and configure serialization to handle object cycles
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

#endregion

#region Swagger

// Configure Swagger and support for JWT authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hotel Booking API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

#endregion

#region Authentication and Infrastructure

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();

#endregion

#region Database

// Configure Dapper
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
var dataSourceBuilder = new NpgsqlDataSourceBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection"));
var dataSource = dataSourceBuilder.Build();
builder.Services.AddSingleton(dataSource);

builder.Services.AddSingleton(new DapperDbContext(dataSource));

#endregion

#region MongoDB Logging
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<ILogRepository, MongoLogRepository>();
#endregion

#region Redis and Cache

// Register Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var configuration = provider.GetService<IConfiguration>();
    var connectionString = configuration?.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(connectionString ?? throw new InvalidOperationException("Redis connection string not configured"));
});

// Register distributed cache using Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(10);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    }
);

// Register cache service abstraction
builder.Services.AddScoped<ICacheService, RedisCacheService>();

#endregion

#region Pub/Sub

builder.Services.AddSingleton<ICacheInvalidationPublisher, CacheInvalidationPublisher>();

builder.Services.AddHostedService<CacheInvalidationSubscriber>();

#endregion

#region Repositories
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IHotelRepository, HotelRepository>();
builder.Services.AddScoped<IRepository<RoomType>, RoomTypeRepository>();
builder.Services.AddScoped<IRepository<RoomPricing>, RoomPricingRepository>();
builder.Services.AddScoped<IRepository<HotelPhoto>, HotelPhotoRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserActionAuditRepository, UserActionAuditRepository>();

#endregion

#region Services and Decorators

// Register main services for use inside decorators
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<HotelService>();

builder.Services.AddScoped<UserService>();

// Use cached decorator implementations instead of standard services
builder.Services.AddScoped<IBookingService>(provider =>
    new CachedBookingService(
        provider.GetRequiredService<BookingService>(),
        provider.GetRequiredService<ICacheService>(),
        provider.GetRequiredService<ILogger<CachedBookingService>>(),
        provider.GetRequiredService<ICacheInvalidationPublisher>())
);

builder.Services.AddScoped<IHotelService>(provider =>
    new CachedHotelService(
        provider.GetRequiredService<HotelService>(),
        provider.GetRequiredService<ICacheService>(),
        provider.GetRequiredService<ILogger<CachedHotelService>>(),
        provider.GetRequiredService<ICacheInvalidationPublisher>())
);


builder.Services.AddScoped<IUserService>(provider =>
    new CachedUserService(
        provider.GetRequiredService<UserService>(),
        provider.GetRequiredService<ICacheService>(),
        provider.GetRequiredService<ILogger<CachedUserService>>(),
        provider.GetRequiredService<ICacheInvalidationPublisher>())
);

builder.Services.AddScoped<IRoomTypeService, RoomTypeService>();
builder.Services.AddScoped<IRoomPricingService, RoomPricingService>();
builder.Services.AddScoped<IUserActionAuditService, UserActionAuditService>();
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IReportingService, ReportingService>();


#endregion

#region Cloudinary

// Cloudinary configuration and repository
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<IPhotoRepository>(provider =>
{
    var settings = provider.GetRequiredService<IOptions<CloudinarySettings>>().Value;
    return new CloudinaryPhotoRepository(settings);
});

builder.Services.AddScoped<IHotelPhotoService, HotelPhotoService>();

#endregion

#region CORS

// Configure CORS for allowed origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policyBuilder =>
        policyBuilder
            .WithOrigins(
                "http://localhost:5044",
                "http://localhost:3000" // frontend
                                        // "https://localhost:7050",
                                        // "http://localhost:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

#endregion

#region JWT

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(builder.Configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"))),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Extract token from cookie or Authorization header
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // context.Token = context.Request.Cookies["X-Access-Token"];
                context.Token = context.HttpContext.Session.GetString("jwt_token");
                if (string.IsNullOrEmpty(context.Token))
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                    {
                        context.Token = authHeader.Substring("Bearer ".Length);
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

#endregion

var app = builder.Build();

// Минималистичный SPA из wwwroot (index.html + app.js + style.css)
app.UseDefaultFiles();
app.UseStaticFiles();

#region Development Tools and Database Seeding



#endregion

#region Middleware

// Enable Swagger always to simplify debugging
app.UseSwagger();
app.UseSwaggerUI();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Hotel Booking API")
        .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl)
        .WithOpenApiRoutePattern("/swagger/v1/swagger.json");
});

// Log requests to see failing endpoints and status codes
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

#endregion

app.Run();

// маркер для WebApplicationFactory в интеграционных тестах
public partial class Program { }
