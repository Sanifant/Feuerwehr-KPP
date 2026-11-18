namespace de.openelp.feuerwehr.Server.Services.Settings
{
    using de.openelp.feuerwehr.Common.Models.Settings;

    /// <summary>Admin-only management of global settings.</summary>
    public interface ISettingsService
    {
        Task<ServiceResult> GetDefinitionsAsync(CancellationToken cancellationToken);

        Task<ServiceResult> GetAsync(CancellationToken cancellationToken);

        Task<ServiceResult> CreateAsync(CreateSettingRequest request, CancellationToken cancellationToken);

        Task<ServiceResult> UpdateAsync(Guid id, UpdateSettingRequest request, CancellationToken cancellationToken);

        Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken);
    }
}
