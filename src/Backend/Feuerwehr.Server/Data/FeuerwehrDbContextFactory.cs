using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace de.openelp.feuerwehr.Server.Data
{
    public class FeuerwehrDbContextFactory : IDesignTimeDbContextFactory<FeuerwehrDbContext>
    {
        /// <summary>
        /// Creates the db Context.
        /// </summary>
        /// <param name="args">The args.</param>
        public FeuerwehrDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<FeuerwehrDbContext> optionsBuilder = new DbContextOptionsBuilder<FeuerwehrDbContext>();
            optionsBuilder.UseNpgsql("Host=localhost;Database=feuerwehrdb;Username=postgres;Password=yourpassword");
            return new FeuerwehrDbContext(optionsBuilder.Options);
        }
    }
}

