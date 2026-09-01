using Feuerwehr.Server.Authorization;
using Feuerwehr.Server.Data;
using Feuerwehr.Server.Models;
using Feuerwehr.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using System.Net.Mail;
using System.Text;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults();

        builder.AddRedisClientBuilder("cache")
            .WithOutputCache();

        builder.AddNpgsqlDbContext<FeuerwehrDbContext>(connectionName: "postgresdb");

        builder.Services.AddSingleton<SmtpClient>(_ =>
        new SmtpClient(
            host: Environment.GetEnvironmentVariable("MAIL_HOST") ?? "localhost",
            port: int.TryParse(
                Environment.GetEnvironmentVariable("MAIL_PORT"), out var p) ? p : 1025));

        // Configure ASP.NET Core Identity
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 8;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<FeuerwehrDbContext>()
        .AddDefaultTokenProviders();

        // Configure JWT Authentication
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured in appsettings.json");

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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero // Remove default 5 minute tolerance
            };
        });

        // Configure Authorization Policies
        builder.Services.AddAuthorization(options =>
        {
            options.AddFeuerwehrPolicies();
        });

        // Register TokenService
        builder.Services.AddScoped<ITokenService, TokenService>();

        // Configure CORS to allow frontend access
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                // In development, allow Aspire's dynamic URLs
                if (builder.Environment.IsDevelopment())
                {
                    policy.SetIsOriginAllowed(origin =>
                    {
                        // Allow localhost and Aspire dev domains
                        var uri = new Uri(origin);
                        return uri.Host == "localhost" ||
                               uri.Host.EndsWith(".dev.localhost") ||
                               uri.Host.EndsWith(".localhost");
                    })
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials(); // Wichtig für Cookies/Auth-Headers
                }
                else
                {
                    // In production: nur spezifische Origins erlauben
                    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                                         ?? Array.Empty<string>();
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
            });
        });

        // Add services to the container.
        builder.Services.AddProblemDetails();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddControllers();

        var app = builder.Build();

        await ApplyMigrationsAsync(app.Services);
        await SeedDatabaseAsync(app.Services);

        // Configure the HTTP request pipeline.
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("My Hydrant API")
                       .WithTheme(ScalarTheme.DeepSpace)
                       .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });

            app.UseDeveloperExceptionPage();
        }

        app.UseCors("AllowFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseOutputCache();

        app.MapControllers();

        app.MapDefaultEndpoints();

        app.Run();
    }

    private static async Task ApplyMigrationsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    private static async Task SeedDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        await DbInitializer.SeedDataAsync(scope.ServiceProvider);
    }
}

