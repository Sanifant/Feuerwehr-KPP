namespace De.Openelp.Feuerwehr.Server.Services.IncidentModules
{
    public interface IUserContextAccessor
    {
        string UserId { get; }

        string DisplayName { get; }

        bool IsAdmin { get; }
    }
}
