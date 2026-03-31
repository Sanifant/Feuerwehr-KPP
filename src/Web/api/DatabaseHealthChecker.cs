using de.openelp.feuerwehr.infrastructure;

namespace de.openelp.feuerwehr.Api
{
    public class DatabaseHealthChecker : IDatabaseHealthChecker
    {
        private readonly AppDbContext _dbContext;

        public DatabaseHealthChecker(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> CanConnectAsync()
        {
            try
            {
                return await _dbContext.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }
    }
}
