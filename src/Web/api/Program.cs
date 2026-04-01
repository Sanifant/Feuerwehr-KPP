using de.openelp.feuerwehr.application.hydrant;
using de.openelp.feuerwehr.application.inventory;
using de.openelp.feuerwehr.infrastructure;
using de.openelp.feuerwehr.Api.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

builder.Services.AddScoped<IHydrantRepository, HydrantRepository>();
builder.Services.AddScoped<HydrantService>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(StatusResponseWriter.DatabaseHealthCheckName);

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

app.Run();
