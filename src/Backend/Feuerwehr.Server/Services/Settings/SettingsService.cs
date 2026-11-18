namespace de.openelp.feuerwehr.Server.Services.Settings
{
    using de.openelp.feuerwehr.Common.Models.Settings;
    using de.openelp.feuerwehr.Server.Data;
    using de.openelp.feuerwehr.Server.Models;
    using de.openelp.feuerwehr.Server.Models.Settings;
    using de.openelp.feuerwehr.Server.Services.IncidentModules;
    using Microsoft.EntityFrameworkCore;
    using Npgsql;

    /// <summary>Persists global settings and validates their typed values.</summary>
    public sealed class SettingsService(FeuerwehrDbContext db, IUserContextAccessor user, SettingsCatalog catalog, IServiceProvider services) : ISettingsService
    {
        public Task<ServiceResult> GetDefinitionsAsync(CancellationToken cancellationToken)
        {
            if (!user.IsAdmin)
            {
                return Task.FromResult(ServiceResult.Forbid());
            }

            List<SettingDefinitionDto> definitions = catalog.Definitions.Select(definition => new SettingDefinitionDto(
                definition.Key, definition.Name, definition.ModuleId,
                catalog.Modules.Single(module => module.Id == definition.ModuleId).Name,
                this.Validator(definition).Type, definition.Mandatory)).OrderBy(x => x.ModuleName).ThenBy(x => x.Name).ToList();
            return Task.FromResult(ServiceResult.Ok(definitions));
        }

        public async Task<ServiceResult> GetAsync(CancellationToken cancellationToken)
        {
            if (!user.IsAdmin)
            {
                return ServiceResult.Forbid();
            }

            string[] keys = catalog.Definitions.Select(x => x.Key).ToArray();
            List<Setting> settings = await db.Settings.AsNoTracking().Where(x => keys.Contains(x.Name)).Include(x => x.ModifiedByUser)
                .OrderBy(x => x.Name).ToListAsync(cancellationToken);
            return ServiceResult.Ok(settings.Select(this.ToDto).OrderBy(x => x.Name).ToList());
        }

        public async Task<ServiceResult> CreateAsync(CreateSettingRequest request, CancellationToken cancellationToken)
        {
            if (!user.IsAdmin)
            {
                return ServiceResult.Forbid();
            }

            SettingDefinition? definition = catalog.Find(request.Key);
            if (definition is null)
            {
                return ServiceResult.Problem(400, "Bitte einen vordefinierten Parameter auswählen.");
            }

            string? error = this.Validator(definition).Validate(request.Value);
            if (error is not null)
            {
                return ServiceResult.Problem(400, error);
            }

            if (await db.Settings.AnyAsync(x => x.Name == definition.Key, cancellationToken))
            {
                return ServiceResult.Problem(409, "Ein Parameter mit diesem Namen existiert bereits.");
            }

            ApplicationUser? editor = await db.Users.FindAsync([user.UserId], cancellationToken);
            if (editor is null)
            {
                return ServiceResult.Unauthorized();
            }

            Setting setting = new Setting
            {
                Id = Guid.NewGuid(),
                Name = definition.Key,
                Value = request.Value,
                Type = this.Validator(definition).Type,
                ModifiedOn = DateTime.UtcNow,
                ModifiedBy = editor.Id,
                ModifiedByUser = editor,
            };
            db.Settings.Add(setting);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Settings_Name" })
            {
                db.Entry(setting).State = EntityState.Detached;
                return ServiceResult.Problem(409, "Ein Parameter mit diesem Namen existiert bereits.");
            }

            return ServiceResult.StatusCode(201, this.ToDto(setting));
        }

        public async Task<ServiceResult> UpdateAsync(Guid id, UpdateSettingRequest request, CancellationToken cancellationToken)
        {
            if (!user.IsAdmin)
            {
                return ServiceResult.Forbid();
            }

            Setting? setting = await db.Settings.FindAsync([id], cancellationToken);
            if (setting is null || catalog.Find(setting.Name) is null)
            {
                return ServiceResult.NotFound();
            }

            string? error = this.Validator(catalog.Find(setting.Name)!).Validate(request.Value);
            if (error is not null)
            {
                return ServiceResult.Problem(400, error);
            }

            ApplicationUser? editor = await db.Users.FindAsync([user.UserId], cancellationToken);
            if (editor is null)
            {
                return ServiceResult.Unauthorized();
            }

            setting.Value = request.Value;
            setting.ModifiedOn = DateTime.UtcNow;
            setting.ModifiedBy = editor.Id;
            setting.ModifiedByUser = editor;
            await db.SaveChangesAsync(cancellationToken);
            return ServiceResult.Ok(this.ToDto(setting));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            if (!user.IsAdmin)
            {
                return ServiceResult.Forbid();
            }

            Setting? setting = await db.Settings.FindAsync([id], cancellationToken);
            if (setting is null || catalog.Find(setting.Name) is null)
            {
                return ServiceResult.NotFound();
            }

            db.Settings.Remove(setting);
            await db.SaveChangesAsync(cancellationToken);
            return ServiceResult.NoContent();
        }

        private SettingDto ToDto(Setting setting)
        {
            SettingDefinition definition = catalog.Find(setting.Name)!;
            return new SettingDto(setting.Id, definition.Key, definition.Name, setting.Value, this.Validator(definition).Type,
                setting.ModifiedOn, setting.ModifiedBy, setting.ModifiedByUser?.FullName ?? string.Empty);
        }

        private ISettingValidator Validator(SettingDefinition definition)
        {
            return (ISettingValidator)services.GetRequiredService(definition.ValidatorType);
        }
    }
}
