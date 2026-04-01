using de.openelp.feuerwehr.application.hydrant;
using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.infrastructure;
using de.openelp.feuerwehr.Api.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.ApplyMigrations();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/api/status", new HealthCheckOptions
{
    ResponseWriter = StatusResponseWriter.WriteAsync
});

// Kubernetes-style liveness probe: checks only that the process is alive
app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Kubernetes-style readiness probe: checks all external dependencies tagged "ready"
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = StatusResponseWriter.WriteAsync
});

app.Run();
