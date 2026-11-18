namespace de.openelp.feuerwehr.Server.Services.Settings
{
    using de.openelp.feuerwehr.Server.Data;
    using de.openelp.feuerwehr.Server.Models;
    using de.openelp.feuerwehr.Server.Models.Settings;
    using Microsoft.EntityFrameworkCore;
    using Npgsql;

    /// <summary>Imports missing parameters once per startup; persisted values always take precedence.</summary>
    public sealed class SettingsEnvironmentInitializer(
        FeuerwehrDbContext db, SettingsCatalog catalog, ISettingsEnvironment environment,
        IServiceProvider services, ILogger<SettingsEnvironmentInitializer> logger)
    {
        public const string InstallerId = "settings-environment-installer";

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            foreach (SettingDefinition definition in catalog.Definitions)
            {
                if (await db.Settings.AnyAsync(x => x.Name == definition.Key, cancellationToken))
                {
                    continue;
                }

                string? value = environment.GetValue(definition.Key);
                if (value is null)
                {
                    continue;
                }

                ISettingValidator validator = (ISettingValidator)services.GetRequiredService(definition.ValidatorType);
                if (validator.Validate(value) is not null)
                {
                    logger.LogWarning("Environment setting {SettingKey} was not imported because validation failed.", definition.Key);
                    continue;
                }

                ApplicationUser installer = await this.GetInstallerAsync(cancellationToken);
                Setting setting = new Setting
                {
                    Id = Guid.NewGuid(), Name = definition.Key, Value = value, Type = validator.Type,
                    ModifiedBy = installer.Id, ModifiedByUser = installer, ModifiedOn = DateTime.UtcNow,
                };
                db.Settings.Add(setting);
                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                    { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Settings_Name" })
                {
                    // Another application instance imported or an administrator created this value first.
                    db.Entry(setting).State = EntityState.Detached;
                }
            }
        }

        private async Task<ApplicationUser> GetInstallerAsync(CancellationToken cancellationToken)
        {
            ApplicationUser? installer = await db.Users.FindAsync([InstallerId], cancellationToken);
            if (installer is not null)
            {
                return installer;
            }

            installer = new ApplicationUser
            {
                Id = InstallerId, UserName = InstallerId, NormalizedUserName = InstallerId.ToUpperInvariant(),
                FirstName = "Installer", IsActive = false, LockoutEnabled = true, LockoutEnd = DateTimeOffset.MaxValue,
                SecurityStamp = Guid.NewGuid().ToString(), PasswordHash = null,
            };
            db.Users.Add(installer);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                db.Entry(installer).State = EntityState.Detached;
                installer = await db.Users.SingleAsync(x => x.Id == InstallerId, cancellationToken);
            }

            return installer;
        }
    }
}
