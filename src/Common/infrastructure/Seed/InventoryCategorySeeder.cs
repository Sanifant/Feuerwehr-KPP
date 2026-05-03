// InventoryCategorySeeder.cs
using de.openelp.feuerwehr.domain;

namespace de.openelp.feuerwehr.infrastructure.Seed
{
    public class InventoryCategorySeeder : ISeeder
    {
        public async Task SeedAsync(AppDbContext context)
        {
            if (context.InventoryCategories.Any()) return; // bereits geseedet

            var categories = new List<InventoryCategory>
            {
                new() { Id = Guid.NewGuid(), Name = "Pumpen",         Description = "Pumpen und Aggregate" },
                new() { Id = Guid.NewGuid(), Name = "Schläuche",      Description = "Schläuche und Kupplungen" },
                new() { Id = Guid.NewGuid(), Name = "Atemschutz",     Description = "Atemschutzgeräte und Zubehör" },
                new() { Id = Guid.NewGuid(), Name = "Werkzeug",       Description = "Handwerkzeug und Maschinen" },
                new() { Id = Guid.NewGuid(), Name = "Schutzkleidung", Description = "PSA und Schutzausrüstung" }
            };

            await context.InventoryCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }
    }
}