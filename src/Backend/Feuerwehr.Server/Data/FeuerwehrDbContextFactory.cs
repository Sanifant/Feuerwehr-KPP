using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Feuerwehr.Server.Data
{
    public class FeuerwehrDbContextFactory: IDesignTimeDbContextFactory<FeuerwehrDbContext>
    {
        public FeuerwehrDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<FeuerwehrDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Database=feuerwehrdb;Username=postgres;Password=yourpassword");
            return new FeuerwehrDbContext(optionsBuilder.Options);
        }
    }
}
