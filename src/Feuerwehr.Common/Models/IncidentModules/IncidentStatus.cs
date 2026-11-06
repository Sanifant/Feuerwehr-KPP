namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Defines the supported incident status values.
    /// </summary>
    public enum IncidentStatus
    {
        /// <summary>
        /// The incident is currently active.
        /// </summary>
        Active = 1,
        /// <summary>
        /// The incident has been closed and is retained in the archive.
        /// </summary>
        Closed = 2,
    }
}
