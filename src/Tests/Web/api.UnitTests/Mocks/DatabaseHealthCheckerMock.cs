using de.openelp.feuerwehr.Api;

namespace de.openelp.feuerwehr.Api.UnitTests.Mocks
{
    internal class DatabaseHealthCheckerMock : IDatabaseHealthChecker
    {
        private readonly bool _canConnect;

        public DatabaseHealthCheckerMock(bool canConnect)
        {
            _canConnect = canConnect;
        }

        public Task<bool> CanConnectAsync()
        {
            return Task.FromResult(_canConnect);
        }
    }
}
