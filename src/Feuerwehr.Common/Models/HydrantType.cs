namespace De.Openelp.Feuerwehr.Common.Models
{
    /// <summary>
    /// Defines the supported hydrant type values.
    /// </summary>
    public enum HydrantType
    {
        /// <summary>
        /// An underground hydrant requiring a standpipe.
        /// </summary>
        Underground = 1,                 // Unterflurhydrant
        /// <summary>
        /// An aboveground hydrant without a drop jacket.
        /// </summary>
        AbovegroundWithoutJack = 2,       // Überflurhydrant ohne Fallmantel
        /// <summary>
        /// An aboveground hydrant with a drop jacket.
        /// </summary>
        AbovegroundWithJack = 3,          // Überflurhydrant mit Fallmantel
        /// <summary>
        /// A wall-mounted hydrant.
        /// </summary>
        WallHydrant = 4                  // Wandhydrant
    }
}
