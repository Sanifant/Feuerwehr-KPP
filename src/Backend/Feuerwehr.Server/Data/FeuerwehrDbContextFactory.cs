using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace De.Openelp.Feuerwehr.Server.Data
{
    /// <summary>
    /// Creates the PostgreSQL context used by Entity Framework design-time commands.
    /// </summary>
    public class FeuerwehrDbContextFactory : IDesignTimeDbContextFactory<FeuerwehrDbContext>
    {
        /// <summary>
        /// Creates the PostgreSQL database context used by Entity Framework design-time tooling.
        /// </summary>
        /// <param name="args">The command-line arguments supplied to the application.</param>
        /// <returns>The configured PostgreSQL database context.</returns>
        public FeuerwehrDbContext CreateDbContext(string[] args)
        {
            DbContextOptionsBuilder<FeuerwehrDbContext> optionsBuilder = new ();
            _ = optionsBuilder.UseNpgsql("Host=localhost;Database=feuerwehrdb;Username=postgres;Password=yourpassword");
            return new FeuerwehrDbContext(optionsBuilder.Options);
        }
    }
}
