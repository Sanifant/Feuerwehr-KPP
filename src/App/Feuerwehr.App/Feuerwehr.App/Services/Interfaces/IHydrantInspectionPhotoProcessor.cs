namespace de.openelp.feuerwehr.App.Services
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using de.openelp.feuerwehr.App.Models;

    /// <summary>Prepares camera or file streams for inspection uploads without disposing the caller's stream.</summary>
    public interface IHydrantInspectionPhotoProcessor
    {
        Task<PreparedHydrantInspectionPhoto> PrepareAsync(Stream source, CancellationToken cancellationToken = default);
    }
}
