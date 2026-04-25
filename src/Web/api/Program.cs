using de.openelp.feuerwehr.application.hydrant;
using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.infrastructure;
using de.openelp.feuerwehr.infrastructure.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. JWT-Validierung konfigurieren
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
{
    throw new InvalidOperationException("JWT Settings fehlen in appsettings.json oder Umgebungsvariablen!");
}

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

// Configure logging providers
builder.Logging.ClearProviders(); // Remove default providers
builder.Logging.AddConsole();     // Add console logging
builder.Logging.AddSystemdConsole(); // Add systemd console logging for better integration with systemd journal

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,

            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateLifetime = true, // Prüft Ablaufzeit (exp claim)
            ClockSkew = TimeSpan.Zero // Keine Toleranz bei Ablauf (strenger)
        };
        options.IncludeErrorDetails = true;
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    // Token ist abgelaufen -> Client sollte Refresh-Token nutzen
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                return Task.CompletedTask;
            }
        };
    });


builder.Services.AddAuthorization();
// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<DatabaseSeeder>();

builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

builder.Services.AddScoped<IHydrantRepository, HydrantRepository>();
builder.Services.AddScoped<HydrantService>();

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("Connection string 'Redis' not found.")));

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(StatusResponseWriter.DatabaseHealthCheckName, tags: ["ready"])
    .AddCheck<RedisHealthCheck>(StatusResponseWriter.RedisHealthCheckName, failureStatus: HealthStatus.Degraded, tags: ["ready"]);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expiration = DateTime.UtcNow.AddMinutes(15);
    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "admin"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "User"),
                new Claim(ClaimTypes.Role, "Admin")
            };

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: expiration,
        signingCredentials: credentials
    );
    var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

    app.MapOpenApi();
    app.MapScalarApiReference()
        .WithSummary(accessToken)
        .WithDescription("API-Dokumentation für die Feuerwehr-App");
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    Console.WriteLine("Applying migrations and seeding database...");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();
