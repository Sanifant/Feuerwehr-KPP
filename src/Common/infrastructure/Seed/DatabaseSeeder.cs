// DatabaseSeeder.cs
using Microsoft.Extensions.Logging;

namespace de.openelp.feuerwehr.infrastructure.Seed
{
    public class DatabaseSeeder
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(AppDbContext context, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger  = logger;
        }

        public async Task SeedAsync()
        {
            // Reihenfolge wichtig! Abhängigkeiten zuerst
            var seeders = new List<ISeeder>
            {
                new InventoryCategorySeeder(),   // keine Abhängigkeiten
                new RelationshipLabelSeeder(),   // keine Abhängigkeiten
            };

            foreach (var seeder in seeders)
            {
                var name = seeder.GetType().Name;
                try
                {
                    _logger.LogInformation("Starte Seeder: {Name}", name);
                    await seeder.SeedAsync(_context);
                    _logger.LogInformation("Seeder abgeschlossen: {Name}", name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler im Seeder: {Name}", name);
                    throw;
                }
            }
        }
    }
}