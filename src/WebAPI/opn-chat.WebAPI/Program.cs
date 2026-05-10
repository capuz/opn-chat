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
    {
        // For PostgreSQL (future migration)
        // options.UseNpgsql(connectionString);
        throw new NotImplementedException("PostgreSQL provider not configured yet. Use SQLite for now.");
    }
    else
    {
        // SQLite (default for development/initial phase)
        options.UseSqlite(connectionString);
    }
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
            policy.WithOrigins("https://your-production-domain.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Authorization");
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

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    // Apply schema migrations not covered by EnsureCreated
    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"Users\" ADD COLUMN \"NicknameChangeCount\" INTEGER NOT NULL DEFAULT 0");
        Console.WriteLine("[MIGRATION] NicknameChangeCount column added.");
    }
    catch (Exception migEx)
    {
        Console.WriteLine($"[MIGRATION] NicknameChangeCount: {migEx.Message}");
    }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"Users\" ADD COLUMN \"CountryCode\" TEXT");
        Console.WriteLine("[MIGRATION] CountryCode column added.");
    }
    catch (Exception migEx)
    {
        Console.WriteLine($"[MIGRATION] CountryCode: {migEx.Message}");
    }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"Users\" ADD COLUMN \"ShowFlag\" INTEGER NOT NULL DEFAULT 0");
        Console.WriteLine("[MIGRATION] ShowFlag column added.");
    }
    catch (Exception migEx)
    {
        Console.WriteLine($"[MIGRATION] ShowFlag: {migEx.Message}");
    }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"Messages\" ADD COLUMN \"Type\" INTEGER NOT NULL DEFAULT 0");
        Console.WriteLine("[MIGRATION] Messages.Type column added.");
    }
    catch (Exception migEx)
    {
        Console.WriteLine($"[MIGRATION] Messages.Type: {migEx.Message}");
    }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"Users\" ADD COLUMN \"GlobalBadge\" TEXT");
        Console.WriteLine("[MIGRATION] GlobalBadge column added.");
    }
    catch (Exception migEx)
    {
        Console.WriteLine($"[MIGRATION] GlobalBadge: {migEx.Message}");
    }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"PrivateMessages\" ADD COLUMN \"IsDeletedBySender\" INTEGER NOT NULL DEFAULT 0");
        Console.WriteLine("[MIGRATION] IsDeletedBySender column added.");
    }
    catch (Exception migEx)
    {
        Console.WriteLine($"[MIGRATION] IsDeletedBySender: {migEx.Message}");
    }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"PrivateMessages\" ADD COLUMN \"IsDeletedByReceiver\" INTEGER NOT NULL DEFAULT 0");
        Console.WriteLine("[MIGRATION] IsDeletedByReceiver column added.");
    }
    catch (Exception migEx)
    {
        Console.WriteLine($"[MIGRATION] IsDeletedByReceiver: {migEx.Message}");
    }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"PrivateMessages\" ADD COLUMN \"IsDeletedForEveryone\" INTEGER NOT NULL DEFAULT 0");
        Console.WriteLine("[MIGRATION] IsDeletedForEveryone column added.");
    }
    catch (Exception migEx)
    {
        Console.WriteLine($"[MIGRATION] IsDeletedForEveryone: {migEx.Message}");
    }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"PrivateMessages\" ADD COLUMN \"DeletedAt\" TEXT");
        Console.WriteLine("[MIGRATION] DeletedAt column added.");
    }
    catch (Exception migEx)
    {
        Console.WriteLine($"[MIGRATION] DeletedAt: {migEx.Message}");
    }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"Users\" ADD COLUMN \"IsAdmin\" INTEGER NOT NULL DEFAULT 0");
        Console.WriteLine("[MIGRATION] Users.IsAdmin column added.");
    }
    catch (Exception migEx) { Console.WriteLine($"[MIGRATION] Users.IsAdmin: {migEx.Message}"); }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"Users\" ADD COLUMN \"IsDeactivated\" INTEGER NOT NULL DEFAULT 0");
        Console.WriteLine("[MIGRATION] Users.IsDeactivated column added.");
    }
    catch (Exception migEx) { Console.WriteLine($"[MIGRATION] Users.IsDeactivated: {migEx.Message}"); }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"Rooms\" ADD COLUMN \"IsLocked\" INTEGER NOT NULL DEFAULT 0");
        Console.WriteLine("[MIGRATION] Rooms.IsLocked column added.");
    }
    catch (Exception migEx) { Console.WriteLine($"[MIGRATION] Rooms.IsLocked: {migEx.Message}"); }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"Rooms\" ADD COLUMN \"IsSystem\" INTEGER NOT NULL DEFAULT 0");
        Console.WriteLine("[MIGRATION] Rooms.IsSystem column added.");
    }
    catch (Exception migEx) { Console.WriteLine($"[MIGRATION] Rooms.IsSystem: {migEx.Message}"); }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"Rooms\" ADD COLUMN \"IsArchived\" INTEGER NOT NULL DEFAULT 0");
        Console.WriteLine("[MIGRATION] Rooms.IsArchived column added.");
    }
    catch (Exception migEx) { Console.WriteLine($"[MIGRATION] Rooms.IsArchived: {migEx.Message}"); }

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE \"Rooms\" ADD COLUMN \"LastActivityAt\" TEXT");
        Console.WriteLine("[MIGRATION] Rooms.LastActivityAt column added.");
    }
    catch (Exception migEx) { Console.WriteLine($"[MIGRATION] Rooms.LastActivityAt: {migEx.Message}"); }

    // Rename and mark system rooms
    try
    {
        context.Database.ExecuteSqlRaw("UPDATE \"Rooms\" SET \"Name\"='#general', \"IsSystem\"=1 WHERE \"Id\"='11111111-1111-1111-1111-111111111111'");
        context.Database.ExecuteSqlRaw("UPDATE \"Rooms\" SET \"Name\"='#random',  \"IsSystem\"=1 WHERE \"Id\"='22222222-2222-2222-2222-222222222222'");
        context.Database.ExecuteSqlRaw("UPDATE \"Rooms\" SET \"Name\"='#help',    \"IsSystem\"=1 WHERE \"Id\"='33333333-3333-3333-3333-333333333333'");
        Console.WriteLine("[MIGRATION] System rooms renamed to IRC style.");
    }
    catch (Exception migEx) { Console.WriteLine($"[MIGRATION] System rooms rename: {migEx.Message}"); }

    try
    {
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""AdminAuditLogs"" (
            ""Id"" TEXT NOT NULL PRIMARY KEY,
            ""AdminId"" TEXT NOT NULL,
            ""AdminNickname"" TEXT NOT NULL DEFAULT '',
            ""Action"" TEXT NOT NULL DEFAULT '',
            ""TargetType"" TEXT,
            ""TargetId"" TEXT,
            ""TargetDisplay"" TEXT,
            ""Details"" TEXT,
            ""Timestamp"" TEXT NOT NULL DEFAULT ''
        )");
        Console.WriteLine("[MIGRATION] AdminAuditLogs table created.");
    }
    catch (Exception migEx) { Console.WriteLine($"[MIGRATION] AdminAuditLogs: {migEx.Message}"); }

    try
    {
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""SystemSettings"" (
            ""Key"" TEXT NOT NULL PRIMARY KEY,
            ""Value"" TEXT
        )");
        Console.WriteLine("[MIGRATION] SystemSettings table created.");
    }
    catch (Exception migEx) { Console.WriteLine($"[MIGRATION] SystemSettings: {migEx.Message}"); }

    var defaultSettings = new[]
    {
        ("MaxNicknameChanges", "3"),
        ("AllowPrivateChats", "true"),
        ("AllowRoomCreation", "true"),
        ("MaintenanceMode", "false"),
        ("SpamThreshold", "5"),
        ("GlobalAnnouncementBanner", "")
    };
    foreach (var (key, value) in defaultSettings)
    {
        try
        {
            context.Database.ExecuteSqlRaw(
                $"INSERT OR IGNORE INTO \"SystemSettings\" (\"Key\", \"Value\") VALUES ('{key}', '{value}')");
        }
        catch { /* ignore */ }
    }

    try
    {
        context.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ""CommandPermissions"" (
            ""CommandName"" TEXT NOT NULL PRIMARY KEY,
            ""Description"" TEXT NOT NULL DEFAULT '',
            ""Syntax"" TEXT NOT NULL DEFAULT '',
            ""Category"" TEXT NOT NULL DEFAULT '',
            ""Examples"" TEXT NOT NULL DEFAULT '',
            ""MemberAllowed"" INTEGER NOT NULL DEFAULT 0,
            ""OperatorAllowed"" INTEGER NOT NULL DEFAULT 0,
            ""FounderAllowed"" INTEGER NOT NULL DEFAULT 0,
            ""AdminAllowed"" INTEGER NOT NULL DEFAULT 0,
            ""IsDangerous"" INTEGER NOT NULL DEFAULT 0,
            ""IsSystem"" INTEGER NOT NULL DEFAULT 0,
            ""IsDeprecated"" INTEGER NOT NULL DEFAULT 0
        )");
        Console.WriteLine("[MIGRATION] CommandPermissions table created.");
    }
    catch (Exception migEx) { Console.WriteLine($"[MIGRATION] CommandPermissions: {migEx.Message}"); }

    var defaultPerms = opn_chat.Infrastructure.Services.AdminService.DefaultPermissions();
    foreach (var p in defaultPerms)
    {
        try
        {
            context.Database.ExecuteSqlRaw(
                $"INSERT OR IGNORE INTO \"CommandPermissions\" (\"CommandName\",\"Description\",\"Syntax\",\"Category\",\"Examples\"," +
                $"\"MemberAllowed\",\"OperatorAllowed\",\"FounderAllowed\",\"AdminAllowed\",\"IsDangerous\",\"IsSystem\",\"IsDeprecated\") " +
                $"VALUES ('{p.CommandName}','{p.Description.Replace("'","''")}','{p.Syntax.Replace("'","''")}','{p.Category}'," +
                $"'{p.Examples.Replace("'","''")}',{(p.MemberAllowed?1:0)},{(p.OperatorAllowed?1:0)},{(p.FounderAllowed?1:0)}," +
                $"{(p.AdminAllowed?1:0)},{(p.IsDangerous?1:0)},{(p.IsSystem?1:0)},{(p.IsDeprecated?1:0)})");
        }
        catch { /* ignore — already seeded */ }
    }
    Console.WriteLine("[SEED] CommandPermissions seeded.");

    var seedEmail = app.Configuration["Admin:SeedEmail"];
    if (!string.IsNullOrWhiteSpace(seedEmail))
    {
        try
        {
            context.Database.ExecuteSqlInterpolated(
                $"UPDATE \"Users\" SET \"IsAdmin\" = 1 WHERE \"Email\" = {seedEmail}");
            Console.WriteLine($"[MIGRATION] Admin seeded for {seedEmail}.");
        }
        catch (Exception migEx) { Console.WriteLine($"[MIGRATION] Admin seed: {migEx.Message}"); }
    }

    // Seed rooms if they don't exist
    if (!context.Rooms.Any())
    {
        var rooms = new[]
        {
            new opn_chat.Domain.Entities.Room { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Global Chat", Description = "Main chat room", IsPrivate = false },
            new opn_chat.Domain.Entities.Room { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Random", Description = "Off-topic discussions", IsPrivate = false },
            new opn_chat.Domain.Entities.Room { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Support", Description = "Get help here", IsPrivate = false },
        };
        context.Rooms.AddRange(rooms);
        await context.SaveChangesAsync();
    }
}

// Health check endpoint to verify structure works
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck");

app.Run();
