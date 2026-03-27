using de.openelp.feuerwehr.domain;
using Microsoft.EntityFrameworkCore;

namespace de.openelp.feuerwehr.infrastructure
{
    public class AppDbContext: DbContext
    {
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<Hydrant> Hydrants { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
    }
}
