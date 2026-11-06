namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    /// <summary>
    /// Defines editor lease expiry and heartbeat intervals shared by incident modules.
    /// </summary>
    public static class IncidentModuleTime
    {
        /// <summary>
        /// The the duration for which an editor lease remains valid without renewal.
        /// </summary>
        public static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(120);
        /// <summary>
        /// The the interval between editor heartbeat requests.
        /// </summary>
        public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);
    }
}
