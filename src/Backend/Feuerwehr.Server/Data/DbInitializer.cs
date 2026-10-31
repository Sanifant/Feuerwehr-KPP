using De.Openelp.Feuerwehr.Common.Models.IncidentModules;
using De.Openelp.Feuerwehr.Server.Authorization;
using De.Openelp.Feuerwehr.Server.Models;
using De.Openelp.Feuerwehr.Server.Models.IncidentModules;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace De.Openelp.Feuerwehr.Server.Data
{
    public static class DbInitializer
    {
        /// <summary>
        /// Executes seed Data.
        /// </summary>
        /// <param name="serviceProvider">The service Provider.</param>
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            RoleManager<IdentityRole> roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            UserManager<ApplicationUser> userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            FeuerwehrDbContext dbContext = serviceProvider.GetRequiredService<FeuerwehrDbContext>();
            ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();

            try
            {
                // Seed Roles
                await SeedRolesAsync(roleManager, logger);

                // Seed Default Admin User
                await SeedAdminUserAsync(userManager, configuration, logger);
                await SeedDiaryCategoriesAsync(dbContext, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            logger.LogInformation("Seeding roles...");

            foreach (string roleName in Roles.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    IdentityResult result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"Role '{roleName}' created successfully");
                    }
                    else
                    {
                        logger.LogError($"Failed to create role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    logger.LogInformation($"Role '{roleName}' already exists");
                }
            }
        }

        private static async Task SeedDiaryCategoriesAsync(FeuerwehrDbContext dbContext, ILogger logger)
        {
            var defaults = new[]
            {
                new { Code = "meldung", Name = "Meldung" },
                new { Code = "auftrag", Name = "Auftrag" },
                new { Code = "rueckmeldung", Name = "Rückmeldung" },
                new { Code = "entscheidung", Name = "Entscheidung" },
                new { Code = "massnahme", Name = "Maßnahme" },
                new { Code = "sonstiges", Name = "Sonstiges" },
            };

            foreach (var cat in defaults)
            {
                DiaryCategory? existing = await dbContext.DiaryCategories.FirstOrDefaultAsync(x => x.Code == cat.Code);
                if (existing is null)
                {
                    dbContext.DiaryCategories.Add(new DiaryCategory
                    {
                        Id = Guid.NewGuid(),
                        Code = cat.Code,
                        Name = cat.Name,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow,
                    });
                    logger.LogInformation("Diary category '{Code}' created.", cat.Code);
                }
            }

            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedAdminUserAsync(
            UserManager<ApplicationUser> userManager, IConfiguration configuration, ILogger logger)
        {
            logger.LogInformation("Seeding default admin user...");

            string? adminEmail = configuration["SeedAdmin:Email"]?.Trim();
            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                adminEmail = "admin@feuerwehr.local";
            }

            ApplicationUser? adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                string? adminPassword = configuration["SeedAdmin:Password"];
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    throw new InvalidOperationException(
                        "Set SeedAdmin__Password before the first startup to create the initial administrator.");
                }

                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                IdentityResult result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to create the initial administrator. Check SeedAdmin__Email and SeedAdmin__Password against the Identity validation rules. Error codes: {string.Join(", ", result.Errors.Select(e => e.Code))}");
                }

                logger.LogInformation("Initial admin user created: {AdminEmail}", adminEmail);
            }
            else
            {
                logger.LogInformation("Admin user already exists: {AdminEmail}", adminEmail);
            }

            foreach (string? role in new[] { Roles.Admin, Roles.IncidentDiaryEditor, Roles.SituationMapEditor })
            {
                if (!await userManager.IsInRoleAsync(adminUser, role))
                {
                    IdentityResult result = await userManager.AddToRoleAsync(adminUser, role);
                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Failed to assign initial administrator role '{role}'. Error codes: {string.Join(", ", result.Errors.Select(e => e.Code))}");
                    }

                    logger.LogInformation("Added {Role} role to admin user: {AdminEmail}", role, adminEmail);
                }
            }
        }
    }
}

