using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using de.openelp.authentification.Data;
using de.openelp.authentification.Services;
using de.openelp.authentification.Models;

var builder = WebApplication.CreateBuilder(args);

// DB Context
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtService, JwtService>();

// JWT Configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT_SECRET missing");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = key
        };
    });

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    Console.WriteLine("Applying migrations and seeding database...");
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.Migrate();
    //var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    //await seeder.SeedAsync();
    string adminUsername = Environment.GetEnvironmentVariable("AdminUser") ?? string.Empty;
    string adminPassword = Environment.GetEnvironmentVariable("AdminPassword") ?? string.Empty;

    if(!string.IsNullOrEmpty(adminUsername) || !string.IsNullOrEmpty(adminPassword))
    {
        var user = db.Users.FirstOrDefault(u => u.Username == adminUsername);
        if (user == null)
        {
            db.Add(new User
            {
                Username = adminUsername,
                PasswordHash = new PasswordHasher().HashPassword(adminPassword),
                Role = "User;Admin"
            });
            db.SaveChanges();
        }
    }

}

app.Run();