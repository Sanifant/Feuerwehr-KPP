namespace De.Openelp.Feuerwehr.Common.Models.IncidentModules
{
    /// <summary>
    /// Defines the supported diary revision type values.
    /// </summary>
    public enum DiaryRevisionType
    {
        /// <summary>
        /// The revision records the initial diary entry.
        /// </summary>
        Created = 1,
        /// <summary>
        /// The revision records an edit to an existing diary entry.
        /// </summary>
        Edited = 2,
        /// <summary>
        /// The reservation or entry has been canceled.
        /// </summary>
        Canceled = 3,
    }
}
