namespace de.openelp.feuerwehr.Server.Services.Settings
{
    using System.Net.Mail;
    using de.openelp.feuerwehr.Common.Models.Settings;
    using de.openelp.feuerwehr.Server.Data;
    using de.openelp.feuerwehr.Server.Models;
    using de.openelp.feuerwehr.Server.Models.Settings;
    using de.openelp.feuerwehr.Server.Services.IncidentModules;
    using Microsoft.EntityFrameworkCore;
    using Npgsql;

    /// <summary>Persists global settings and validates their typed values.</summary>
    public sealed class SettingsService(FeuerwehrDbContext db, IUserContextAccessor user) : ISettingsService
    {
        public async Task<ServiceResult> GetAsync(CancellationToken cancellationToken)
        {
            if (!user.IsAdmin)
            {
                return ServiceResult.Forbid();
            }

            List<Setting> settings = await db.Settings.AsNoTracking().Include(x => x.ModifiedByUser)
                .OrderBy(x => x.Name).ToListAsync(cancellationToken);
            return ServiceResult.Ok(settings.Select(ToDto).ToList());
        }

        public async Task<ServiceResult> CreateAsync(CreateSettingRequest request, CancellationToken cancellationToken)
        {
            if (!user.IsAdmin)
            {
                return ServiceResult.Forbid();
            }

            string name = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            {
                return ServiceResult.Problem(400, "Bitte einen Parameternamen mit maximal 200 Zeichen angeben.");
            }

            if (string.Equals(name, "Hydrant Benachrichtigung", StringComparison.OrdinalIgnoreCase)
                && request.Type != SettingType.Email)
            {
                return ServiceResult.Problem(400, "Hydrant Benachrichtigung muss den Typ E-Mail haben.");
            }

            string? error = Validate(request.Type, request.Value);
            if (error is not null)
            {
                return ServiceResult.Problem(400, error);
            }

            if (await db.Settings.AnyAsync(x => x.Name == name, cancellationToken))
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
                Name = name,
                Value = request.Value,
                Type = request.Type,
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

            return ServiceResult.StatusCode(201, ToDto(setting));
        }

        public async Task<ServiceResult> UpdateAsync(Guid id, UpdateSettingRequest request, CancellationToken cancellationToken)
        {
            if (!user.IsAdmin)
            {
                return ServiceResult.Forbid();
            }

            Setting? setting = await db.Settings.FindAsync([id], cancellationToken);
            if (setting is null)
            {
                return ServiceResult.NotFound();
            }

            string? error = Validate(setting.Type, request.Value);
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
            return ServiceResult.Ok(ToDto(setting));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            if (!user.IsAdmin)
            {
                return ServiceResult.Forbid();
            }

            Setting? setting = await db.Settings.FindAsync([id], cancellationToken);
            if (setting is null)
            {
                return ServiceResult.NotFound();
            }

            db.Settings.Remove(setting);
            await db.SaveChangesAsync(cancellationToken);
            return ServiceResult.NoContent();
        }

        private static SettingDto ToDto(Setting setting)
        {
            return new SettingDto(setting.Id, setting.Name, setting.Value, setting.Type,
                setting.ModifiedOn, setting.ModifiedBy, setting.ModifiedByUser?.FullName ?? string.Empty);
        }

        private static string? Validate(SettingType type, string? value)
        {
            if (!Enum.IsDefined(type))
            {
                return "Bitte einen gültigen Parametertyp auswählen.";
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return "Bitte einen Wert angeben.";
            }

            if (type == SettingType.Number
                && (value.Any(character => character < '0' || character > '9') || !value.Any(character => character != '0')))
            {
                return "Bitte eine positive natürliche Zahl größer als Null ohne Vorzeichen oder Dezimalstellen angeben.";
            }

            if (type == SettingType.Email
                && (!MailAddress.TryCreate(value, out MailAddress? address)
                    || !string.Equals(address.Address, value, StringComparison.Ordinal)))
            {
                return "Bitte genau eine gültige E-Mail-Adresse ohne Anzeigenamen angeben.";
            }

            return null;
        }
    }
}
