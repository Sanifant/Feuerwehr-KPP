using System.ComponentModel.DataAnnotations;

namespace De.Openelp.Feuerwehr.Common.Models.Vehicles
{
    /// <summary>
    /// Executes vehicle Import Error.
    /// </summary>
    /// <param name="LineNumber">The line Number.</param>
    /// <param name="Description">The description.</param>
    public sealed record VehicleImportError(long? LineNumber, string Description);
}
