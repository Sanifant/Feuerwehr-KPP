namespace de.openelp.feuerwehr.Api
{
    public interface IDatabaseHealthChecker
    {
        Task<bool> CanConnectAsync();
    }
}
