namespace De.Openelp.Feuerwehr.Common.Models
{
    /// <summary>
    /// Defines the supported water source values.
    /// </summary>
    public enum WaterSource
    {
        /// <summary>
        /// Water is supplied by the public water distribution network.
        /// </summary>
        WaterGrid = 1,                   // Trinkwassernetz
        /// <summary>
        /// Water is drawn from a fire pond.
        /// </summary>
        FirePond = 2,                    // Löschwasserteich
        /// <summary>
        /// Water is drawn from a firefighting well.
        /// </summary>
        FireWell = 3,                    // Löschwasserbrunnen
        /// <summary>
        /// Water is stored in a cistern.
        /// </summary>
        Cistern = 4                      // Zisterne
    }
}
