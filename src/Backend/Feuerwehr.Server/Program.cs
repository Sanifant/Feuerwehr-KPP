using de.openelp.feuerwehr.Server.Authorization;
using de.openelp.feuerwehr.Server.Data;
using de.openelp.feuerwehr.Server.Hubs;
using de.openelp.feuerwehr.Server.Models;
using de.openelp.feuerwehr.Server.Services;
using de.openelp.feuerwehr.Server.Services.IncidentModules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using Scalar.AspNetCore;
using MailKit.Net.Smtp;
using System.Text;
using de.openelp.feuerwehr.Diagnostics;

namespace de.openelp.feuerwehr.Server;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        using var diagnostics = ProcessDiagnostics.Start("Server");
        try { await RunAsync(args, diagnostics); }
        catch (Exception error)
        {
            diagnostics.Record("Server.FatalException", exception: error);
            throw;
        }
    }

    private static async Task RunAsync(string[] args, ProcessDiagnostics diagnostics)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add service defaults & Aspire client integrations.
        builder.AddServiceDefaults();

        builder.AddRedisClientBuilder("cache")
            .WithOutputCache();

        builder.AddNpgsqlDbContext<FeuerwehrDbContext>(connectionName: "postgresdb");

        builder.Services.AddScoped(services => SmtpSettings.FromConfiguration(services.GetRequiredService<IConfiguration>()));
        builder.Services.AddScoped<ISmtpClient, SmtpClient>();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<IMailService, MailService>();
        builder.Services.AddScoped<IUserInvitationStore, UserInvitationStore>();
        builder.Services.AddScoped<IUserInvitationService, UserInvitationService>();

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

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/incidents"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var tokens = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();
                    if (context.Principal is null || !await tokens.IsAccessTokenValidAsync(context.Principal))
                        context.Fail("Das Konto oder die Sitzung ist nicht mehr gültig.");
                }
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
        builder.Services.AddSignalR();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserContextAccessor, HttpUserContextAccessor>();
        builder.Services.AddScoped<ICommandIdempotencyService, CommandIdempotencyService>();
        builder.Services.AddScoped<IOutboxService, OutboxService>();
        builder.Services.AddScoped<IMapLeaseService, MapLeaseService>();
        builder.Services.AddHostedService<OutboxDispatcher>();

        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IFireDepartmentService, FireDepartmentService>();
        builder.Services.AddScoped<IHydrantService, HydrantService>();
        builder.Services.AddScoped<IDiaryCategoryAdminService, DiaryCategoryAdminService>();
        builder.Services.AddScoped<IIncidentService, IncidentService>();
        builder.Services.AddScoped<IIncidentDiaryService, IncidentDiaryService>();
        builder.Services.AddScoped<IIncidentArchiveService, IncidentArchiveService>();
        builder.Services.AddScoped<ISituationMapService, SituationMapService>();
        builder.Services.AddScoped<IMapSymbolService, MapSymbolService>();
        builder.Services.AddScoped<ITrainingRecordService, TrainingRecordService>();
        builder.Services.AddScoped<IUsersService, UsersService>();
        builder.Services.AddScoped<IInventoryService, InventoryService>();
        builder.Services.AddScoped<IInventoryTransferService, InventoryTransferService>();
        builder.Services.AddScoped<IInventoryReminderService, InventoryReminderService>();
        builder.Services.AddHostedService<InventoryReminderWorker>();

        var app = builder.Build();

        app.Lifetime.ApplicationStarted.Register(() => diagnostics.Record("Server.ApplicationStarted"));
        app.Lifetime.ApplicationStopping.Register(() => diagnostics.Record("Server.ApplicationStopping"));
        app.Lifetime.ApplicationStopped.Register(() => diagnostics.Record("Server.ApplicationStopped"));

        diagnostics.Record("Server.Migrations.Start");
        await ApplyMigrationsAsync(app.Services);
        diagnostics.Record("Server.Migrations.Complete");
        await SeedDatabaseAsync(app.Services);
        diagnostics.Record("Server.Seeding.Complete");

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
        app.MapHub<IncidentHub>("/hubs/incidents");
        app.MapPrometheusScrapingEndpoint("/metrics");

        app.MapDefaultEndpoints();

        diagnostics.Record("Server.Run.Start");
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
