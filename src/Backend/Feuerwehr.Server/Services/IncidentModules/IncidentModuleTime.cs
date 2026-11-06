namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public static class IncidentModuleTime
    {
        public static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(120);
        public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);
    }
}
