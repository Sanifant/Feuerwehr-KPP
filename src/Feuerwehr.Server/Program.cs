using Feuerwehr.Server.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

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

        // Add services to the container.
        builder.Services.AddProblemDetails();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddControllers();

        var app = builder.Build();

        await ApplyMigrationsAsync(app.Services);

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

        app.UseOutputCache();

        app.MapControllers();

        app.MapDefaultEndpoints();

        app.UseFileServer();

        app.Run();
    }

    private static async Task ApplyMigrationsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FeuerwehrDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}

