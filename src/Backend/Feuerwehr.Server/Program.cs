using System.Text;
using De.Openelp.Feuerwehr.Diagnostics;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Data;
using De.Openelp.Feuerwehr.Server.Hubs;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Services;
using De.Openelp.Feuerwehr.Server.Services.IncidentModules;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

namespace De.Openelp.Feuerwehr.Server
{
    /// <summary>
    /// Exposes the API entry point for hosting and integration testing.
    /// </summary>
    public partial class Program
    {
        private static async Task Main(string[] args)
        {
            using ProcessDiagnostics diagnostics = ProcessDiagnostics.Start("Server");
            try
            {
                await RunAsync(args, diagnostics);
            }
            catch (Exception error)
            {
                diagnostics.Record("Server.FatalException", exception: error);
                throw;
            }
        }

        private static async Task RunAsync(string[] args, ProcessDiagnostics diagnostics)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add service defaults & Aspire client integrations.
            _ = builder.AddServiceDefaults();

            _ = builder.AddRedisClientBuilder("cache")
                .WithOutputCache();

            builder.AddNpgsqlDbContext<FeuerwehrDbContext>(connectionName: "postgresdb");

            _ = builder.Services.AddScoped(services => SmtpSettings.FromConfiguration(services.GetRequiredService<IConfiguration>()));
            _ = builder.Services.AddScoped<ISmtpClient, SmtpClient>();

            _ = builder.Services.AddSingleton(TimeProvider.System);
            _ = builder.Services.AddScoped<IMailService, MailService>();
            _ = builder.Services.AddScoped<IHtmlMailService, MailService>();
            _ = builder.Services.AddScoped<IVehicleService, VehicleService>();
            _ = builder.Services.AddScoped<IVehicleTransferService, VehicleTransferService>();
            _ = builder.Services.AddScoped<IVehicleMailComposer, VehicleMailComposer>();
            _ = builder.Services.AddScoped<IVehicleMailDispatcher, VehicleMailDispatcher>();
            _ = builder.Services.AddHostedService<VehicleMailWorker>();
            _ = builder.Services.AddScoped<IUserInvitationStore, UserInvitationStore>();
            _ = builder.Services.AddScoped<IUserInvitationService, UserInvitationService>();

            // Configure ASP.NET Core Identity
            _ = builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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
            IConfigurationSection jwtSettings = builder.Configuration.GetSection("JwtSettings");
            string secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured in appsettings.json");

            _ = builder.Services.AddAuthentication(options =>
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
                        StringValues accessToken = context.Request.Query["access_token"];
                        PathString path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/incidents"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        ITokenService tokens = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();
                        if (context.Principal is null || !await tokens.IsAccessTokenValidAsync(context.Principal))
                        {
                            context.Fail("Das Konto oder die Sitzung ist nicht mehr gültig.");
                        }
                    }
                };
            });

            // Configure Authorization Policies
            _ = builder.Services.AddAuthorization(options =>
            {
                options.AddFeuerwehrPolicies();
            });

            // Register TokenService
            _ = builder.Services.AddScoped<ITokenService, TokenService>();

            // Configure CORS to allow frontend access
            _ = builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    // In development, allow Aspire's dynamic URLs
                    if (builder.Environment.IsDevelopment())
                    {
                        _ = policy.SetIsOriginAllowed(origin =>
                        {
                            // Allow localhost and Aspire dev domains
                            Uri uri = new (origin);
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
                        string[] allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                                             ?? Array.Empty<string>();
                        _ = policy.WithOrigins(allowedOrigins)
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    }
                });
            });

            // Add services to the container.
            _ = builder.Services.AddProblemDetails();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            _ = builder.Services.AddOpenApi();

            _ = builder.Services.AddControllers();
            _ = builder.Services.AddSignalR();
            _ = builder.Services.AddHttpContextAccessor();
            _ = builder.Services.AddScoped<IUserContextAccessor, HttpUserContextAccessor>();
            _ = builder.Services.AddScoped<ICommandIdempotencyService, CommandIdempotencyService>();
            _ = builder.Services.AddScoped<IOutboxService, OutboxService>();
            _ = builder.Services.AddScoped<IMapLeaseService, MapLeaseService>();
            _ = builder.Services.AddHostedService<OutboxDispatcher>();

            _ = builder.Services.AddScoped<IAuthService, AuthService>();
            _ = builder.Services.AddScoped<IFireDepartmentService, FireDepartmentService>();
            _ = builder.Services.AddScoped<IHydrantService, HydrantService>();
            _ = builder.Services.AddScoped<IDiaryCategoryAdminService, DiaryCategoryAdminService>();
            _ = builder.Services.AddScoped<IIncidentService, IncidentService>();
            _ = builder.Services.AddScoped<IIncidentDiaryService, IncidentDiaryService>();
            _ = builder.Services.AddScoped<IIncidentArchiveService, IncidentArchiveService>();
            _ = builder.Services.AddScoped<ISituationMapService, SituationMapService>();
            _ = builder.Services.AddScoped<IMapSymbolService, MapSymbolService>();
            _ = builder.Services.AddScoped<ITrainingRecordService, TrainingRecordService>();
            _ = builder.Services.AddScoped<IUsersService, UsersService>();
            _ = builder.Services.AddScoped<IInventoryService, InventoryService>();
            _ = builder.Services.AddScoped<IInventoryTransferService, InventoryTransferService>();
            _ = builder.Services.AddScoped<IInventoryReminderService, InventoryReminderService>();
            _ = builder.Services.AddHostedService<InventoryReminderWorker>();

            WebApplication app = builder.Build();

            _ = app.Lifetime.ApplicationStarted.Register(() => diagnostics.Record("Server.ApplicationStarted"));
            _ = app.Lifetime.ApplicationStopping.Register(() => diagnostics.Record("Server.ApplicationStopping"));
            _ = app.Lifetime.ApplicationStopped.Register(() => diagnostics.Record("Server.ApplicationStopped"));

            diagnostics.Record("Server.Migrations.Start");
            await ApplyMigrationsAsync(app.Services);
            diagnostics.Record("Server.Migrations.Complete");
            await SeedDatabaseAsync(app.Services);
            diagnostics.Record("Server.Seeding.Complete");

            // Configure the HTTP request pipeline.
            _ = app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
            {
                _ = app.MapOpenApi();

                _ = app.MapScalarApiReference(options =>
                {
                    _ = options.WithTitle("My Hydrant API")
                           .WithTheme(ScalarTheme.DeepSpace)
                           .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
                });

                _ = app.UseDeveloperExceptionPage();
            }

            _ = app.UseCors("AllowFrontend");

            _ = app.UseAuthentication();
            _ = app.UseAuthorization();

            _ = app.UseOutputCache();

            _ = app.MapControllers();
            _ = app.MapHub<IncidentHub>("/hubs/incidents");
            _ = app.MapPrometheusScrapingEndpoint("/metrics");

            _ = app.MapDefaultEndpoints();

            diagnostics.Record("Server.Run.Start");
            app.Run();
        }

        private static async Task ApplyMigrationsAsync(IServiceProvider services)
        {
            using IServiceScope scope = services.CreateScope();
            FeuerwehrDbContext dbContext = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        private static async Task SeedDatabaseAsync(IServiceProvider services)
        {
            using IServiceScope scope = services.CreateScope();
            await DbInitializer.SeedDataAsync(scope.ServiceProvider);
        }
    }
}
