using Feuerwehr.Server.Authorization;
using Feuerwehr.Server.Models.IncidentModules;
using Feuerwehr.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Feuerwehr.Server.Data
{
    public static class DbInitializer
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var dbContext = serviceProvider.GetRequiredService<FeuerwehrDbContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                // Seed Roles
                await SeedRolesAsync(roleManager, logger);

                // Seed Default Admin User
                await SeedAdminUserAsync(userManager, logger);
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

            foreach (var roleName in Roles.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
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
                var existing = await dbContext.DiaryCategories.FirstOrDefaultAsync(x => x.Code == cat.Code);
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

        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
        {
            logger.LogInformation("Seeding default admin user...");

            const string adminEmail = "admin@feuerwehr.local";
            const string adminPassword = "Admin@123456"; // Change this in production!

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    // Assign Admin role
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                    logger.LogInformation($"Default admin user created: {adminEmail}");
                    logger.LogWarning($"IMPORTANT: Change the default admin password '{adminPassword}' immediately!");
                }
                else
                {
                    logger.LogError($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.LogInformation($"Admin user already exists: {adminEmail}");

                // Ensure admin has Admin role
                if (!await userManager.IsInRoleAsync(existingAdmin, Roles.Admin))
                {
                    await userManager.AddToRoleAsync(existingAdmin, Roles.Admin);
                    logger.LogInformation($"Added Admin role to existing user: {adminEmail}");
                }
            }
        }
    }
}
