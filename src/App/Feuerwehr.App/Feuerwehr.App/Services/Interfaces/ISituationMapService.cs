using System;
using System.Threading;
using System.Threading.Tasks;
using De.Openelp.Feuerwehr.Common.Models.Dto;
using De.Openelp.Feuerwehr.Common.Models.IncidentModules;

namespace De.Openelp.Feuerwehr.App.Services
{
    /// <summary>
    /// Defines the situation map operations required by application consumers.
    /// </summary>
    public interface ISituationMapService
    {
        /// <summary>
        /// Retrieves the current active incident, when one exists.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the active incident summary, or null when there is no active incident.</returns>
        Task<IncidentSummaryDto?> GetActiveIncidentAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the incident's map elements, hydrants, symbols, and current editing lease.
        /// </summary>
        /// <param name="incidentId">The identifier of the incident.</param>
        /// <param name="cancellationToken">A token that cancels the operation.</param>
        /// <returns>A task whose result is the incident's map elements, hydrants, and current editing lease.</returns>
        Task<MapStateResponse> GetStateAsync(Guid incidentId, CancellationToken cancellationToken);
    }
}
