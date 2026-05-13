using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using opn_chat.Application.Interfaces;
using opn_chat.Application.Services;
using opn_chat.Domain.Interfaces;
using opn_chat.Infrastructure.Data;
using opn_chat.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add SignalR
builder.Services.AddSignalR(options => options.EnableDetailedErrors = true);

// Configure JWT authentication
var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "your-super-secret-key-change-this";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "opn-chat";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "opn-chat-client";

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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey))
    };
    // SignalR sends the JWT as ?access_token= because WebSocket connections can't set headers
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

// Register application services
builder.Services.AddScoped<IJwtGenerator>(sp =>
    new JwtGenerator(
        jwtKey,
        jwtIssuer,
        jwtAudience,
        builder.Configuration.GetValue<int>("Jwt:AccessTokenExpiryMinutes", 60)
    )
);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IPrivateMessageService, PrivateMessageService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();
builder.Services.AddSingleton<opn_chat.Application.Interfaces.IBoostService, opn_chat.WebAPI.Services.BoostService>();
builder.Services.AddScoped<opn_chat.Application.Interfaces.IAdminService, opn_chat.Infrastructure.Services.AdminService>();
builder.Services.AddScoped<opn_chat.Domain.Interfaces.ISystemSettingRepository, opn_chat.Infrastructure.Repositories.SystemSettingRepository>();
builder.Services.AddScoped<opn_chat.Domain.Interfaces.ICommandPermissionRepository, opn_chat.Infrastructure.Repositories.CommandPermissionRepository>();
builder.Services.AddHostedService<opn_chat.Infrastructure.Services.RoomArchivalService>();

// Register infrastructure repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomMemberRepository, RoomMemberRepository>();
builder.Services.AddScoped<IPrivateMessageRepository, PrivateMessageRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// Configure EF Core with provider flexibility
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    // Determine provider based on connection string or environment
    var databaseProvider = configuration["DatabaseProvider"]?.ToLowerInvariant() ?? "sqlite";
    
    if (databaseProvider == "postgresql")
        options.UseNpgsql(connectionString);
    else
        options.UseSqlite(connectionString);
});

// Add CORS for frontend (React + Vite — any localhost port in dev)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Authorization")
                  .AllowCredentials();
        }
        else
        {
            var frontendUrl = builder.Configuration["Frontend:Url"] ?? "https://opn-chat.vercel.app";
            policy.WithOrigins(frontendUrl)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Authorization")
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CORS MUST be before Authentication
app.UseCors("AllowFrontend");

// Disable HTTPS redirection for local development without certificates
// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR hubs
app.MapHub<opn_chat.WebAPI.Hubs.ChatHub>("/hubs/chat");
app.MapHub<opn_chat.WebAPI.Hubs.PresenceHub>("/hubs/presence");
app.MapHub<opn_chat.WebAPI.Hubs.NotificationHub>("/hubs/notifications");

app.MapControllers();

// Apply migrations and seed initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // PostgreSQL (prod/Render): fresh DB, EF migrations manage schema
    // SQLite (dev): EnsureCreated preserves existing data; Migrate() would fail
    //               on existing DBs because there's no __EFMigrationsHistory
    if (context.Database.ProviderName?.Contains("Npgsql") == true)
        await context.Database.MigrateAsync();
    else
        context.Database.EnsureCreated();

    Console.WriteLine("[STARTUP] Database ready.");

    // Seed system rooms
    if (!context.Rooms.Any())
    {
        context.Rooms.AddRange(new[]
        {
            new opn_chat.Domain.Entities.Room { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "#general", Description = "Main chat room",       IsPrivate = false, IsSystem = true },
            new opn_chat.Domain.Entities.Room { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "#random",  Description = "Off-topic discussions", IsPrivate = false, IsSystem = true },
            new opn_chat.Domain.Entities.Room { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "#help",    Description = "Get help here",         IsPrivate = false, IsSystem = true },
        });
        await context.SaveChangesAsync();
        Console.WriteLine("[SEED] System rooms created.");
    }

    // Seed SystemSettings
    var defaultSettings = new[]
    {
        ("MaxNicknameChanges",       "3"),
        ("AllowPrivateChats",        "true"),
        ("AllowRoomCreation",        "true"),
        ("MaintenanceMode",          "false"),
        ("SpamThreshold",            "5"),
        ("GlobalAnnouncementBanner", ""),
    };
    foreach (var (key, value) in defaultSettings)
    {
        if (!context.SystemSettings.Any(s => s.Key == key))
            context.SystemSettings.Add(new opn_chat.Domain.Entities.SystemSetting { Key = key, Value = value });
    }
    await context.SaveChangesAsync();
    Console.WriteLine("[SEED] SystemSettings seeded.");

    // Seed CommandPermissions
    var defaultPerms = opn_chat.Infrastructure.Services.AdminService.DefaultPermissions();
    foreach (var p in defaultPerms)
    {
        if (!context.CommandPermissions.Any(cp => cp.CommandName == p.CommandName))
            context.CommandPermissions.Add(p);
    }
    await context.SaveChangesAsync();
    Console.WriteLine("[SEED] CommandPermissions seeded.");

    // Seed admin user
    var seedEmail = app.Configuration["Admin:SeedEmail"];
    if (!string.IsNullOrWhiteSpace(seedEmail))
    {
        var adminUser = context.Users.FirstOrDefault(u => u.Email == seedEmail);
        if (adminUser != null)
        {
            adminUser.IsAdmin = true;
            await context.SaveChangesAsync();
            Console.WriteLine($"[SEED] Admin seeded for {seedEmail}.");
        }
    }
}

// Health check endpoint to verify structure works
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck");

app.Run();
