namespace de.openelp.feuerwehr.Server.Services.IncidentModules
{
    public interface IUserContextAccessor
    {
        string UserId { get; }
        string DisplayName { get; }
        bool IsAdmin { get; }
    }
}
